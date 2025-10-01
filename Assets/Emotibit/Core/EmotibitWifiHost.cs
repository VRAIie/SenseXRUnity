using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SenseXR.Core.Demo.OSC.Emotibit.Utils;

namespace Emotibit.Core
{
	/// <summary>
	/// Struct to hold Emotibit IP address and availability status.
	/// </summary>
	internal struct EmotibitInfo
	{
		public String Ip;

		public bool BIsAvailable;

		public long LastSeen;

		public EmotibitInfo(String inIp, bool bAvailable, long inLastSeen)
		{
			Ip = inIp;
			BIsAvailable = bAvailable;
			LastSeen =  inLastSeen;
		}
	};
	
	/// <summary>
	/// Class to hold Emotibit Wifi functionalities. Most notably it manages advertising and device discovery,
	/// as well as sending and receiving data.
	/// </summary>
    public class EmotibitWifiHost
    {
	    private EmotibitConfig _wifiHostSettings;
	    private const short Success = 0;
	    private const short Fail = -1;

	    private readonly short _startCxnInterval = 100;

	    private Socket _advertisingCxn;
	    private Socket _dataCxn;
	    private Socket _controlCxn;

	    private Thread _dataThread;
	    private Thread _advertisingThread;

	    private static Mutex _controlCxnMutex = new ();
	    private static Mutex _dataCxnMutex = new ();
	    private static Mutex _discoveredEmotibitsMutex = new ();
	    private static Mutex _packetsMutex = new ();

	    private int _advertisingPort;
	    private int _dataPort;
	    private int _sendDataPort;
	    private ushort _controlPort;

	    private ulong _sendAdvertisingTimer = 0;

	    private readonly List<string> _availableNetworks = new (); // All available networks, with or without emotibits
	    private readonly List<string> _emotibitNetworks = new (); // Networks that contain emotibits
	    private bool _enableBroadcast = false;
	    private ulong _advertizingTimer;

	    private readonly Dictionary<string, EmotibitInfo> _discoveredEmotibits = new ();	// list of EmotiBit IP addresses

	    private string _connectedEmotibitIp;
	    // ToDo: Find a scalable solution to store connected EmotiBit details.
	    // Ex. If we want to change the selected emotibit name to be displayed, instead of ID
	    private string _connectedEmotibitIdentifier;  //!< stores the ID of the connected EmotiBit 
	    private bool _isConnected;
	    private bool _isStartingConnection;
	    private ushort _startCxnTimeout = 5000;	// milliseconds
	    private long _startCxnAbortTimer;

	    private ushort _advertisingPacketCounter = 0;
	    private ushort _controlPacketCounter = 0;
	    private ushort _dataPacketCounter = 0;

	    private readonly List<string> _dataPackets = new ();

	    private readonly ushort _pingInterval = 500;
	    private long _connectionTimer;
	    private readonly ushort _connectionTimeout = 10000;
	    private ushort _availabilityTimeout = 5000;
	    private ushort _ipPurgeTimeout = 15000;

	    private bool _stopDataThread = false;
	    private bool _stopAdvertisingThread = false ;
	    private uint _receivedDataPacketNumber = 60000;	
	    
        private readonly Dictionary<string, List<float>> _latestSensorsDataFrame = new();

        private bool _emotibitsFound = false;
        private bool _startNewSend = true;
        private bool _sendInProgress = true;
        private int _unicastNetwork = 0;
        private int _broadcastNetwork = 0;

        public List<string> DataPackets = new List<string>();
        public List<float> SensorData = new List<float>();
        private readonly Action<string> _logger;
        public event Action<string, List<float>, EmotibitHeader> OnDataReceived;

        /// <summary>
        /// Constructor for EmotibitWifiHost. By default, it will load the settings from the config folder and default to
        /// only a single device.
        /// </summary>
        /// <param name="wifiHostSettings">custom host settings</param>
        /// <param name="logger">optional logger function</param>
        /// <param name="deserializer">function to deal with settings deserialization from default path</param>
        public EmotibitWifiHost(EmotibitConfig wifiHostSettings = null, Action<string> logger = null, Func<string,EmotibitConfig> deserializer = null)
        {
	        _wifiHostSettings = wifiHostSettings ?? new EmotibitConfig();
	        _logger = logger;
	        Initialize(deserializer);
	        
        }
        
        /// <summary>
        /// Reinitialize the EmotibitWifiHost. This is useful if you want to change the settings, or if you want to
        /// re-initialize the host after it has been destroyed.
        /// </summary>
        /// <param name="deserializer"></param>
        private void Initialize(Func<string,EmotibitConfig> deserializer)
		{
			// Init sockets or preload anything here
			Reset();
			LoadSettings("", deserializer);
			Begin();
			_emotibitsFound = false;
			_startNewSend = true;
			_sendInProgress = true;
			_unicastNetwork = 0;
			_broadcastNetwork = 0; 
		}

        /// <summary>
        /// Resets the mutexes and counters.
        /// </summary>
		private void Reset()
		{
			EmotibitWifiHost._cnxLastCycleCount = -1;
			EmotibitWifiHost._controlCxnMutex = new Mutex();
			EmotibitWifiHost._dataCxnMutex = new Mutex();
			EmotibitWifiHost._discoveredEmotibitsMutex = new Mutex();
			EmotibitWifiHost._hostId = -1;
			EmotibitWifiHost._lastCycleCount = -1;
			EmotibitWifiHost._packetsMutex = new Mutex();
			EmotibitWifiHost._pingLastCycleCount = -1;
			EmotibitWifiHost._sendLastCycleCount = -1;
			EmotibitWifiHost._unicastLastCycleCount = -1;
		}

		private readonly List<string> _splitBuffer = new List<string>(64);
		/// <summary>
		/// Function to split a string into a list of strings based on a delimiter using pre-allocated buffer.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="delimiter"></param>
		/// <returns></returns>
		private List<string> SplitString(string str, char delimiter)
		{
			// Avoid allocations from string.Split + ToList by filling the reusable buffer
			_splitBuffer.Clear();
			if (string.IsNullOrEmpty(str))
				return _splitBuffer;

			int start = 0;
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == delimiter)
				{
					if (i > start)
						_splitBuffer.Add(str.Substring(start, i - start)); // consider Span if moving to newer runtime
					// skip empty tokens; if you need them, remove this condition and Add empty string
					start = i + 1;
				}
			}
			if (start < str.Length)
				_splitBuffer.Add(str.Substring(start));
			return _splitBuffer;
		}

		/// <summary>
		/// Tick function used to sample collected data.
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Tick(float deltaTime)
		{
			ReadData(ref DataPackets);
			if (DataPackets == null) return;
			_latestSensorsDataFrame.Clear();
			
			foreach (string packet in DataPackets)
			{
				if (GetWifiHostSettings().DebugLogs)
				{
					_logger("EmotiBit packet: " + packet);
				}
				List<string> responsePackets = SplitString(packet, ',');
				ProcessSlowResponseMessage(responsePackets);
			}
			DataPackets.Clear();
		}

		/// <summary>
		/// Processes collected data from the data thread. Invokes the OnDataReceived event with the parsed data (sensor and values)
		/// </summary>
		/// <param name="responsePackets"></param>
		private void ProcessSlowResponseMessage(List<string> responsePackets)
		{
			EmotibitHeader packetHeader = default;
			ushort maxBufferLength = 64;
			if (EmotibitPacket.getHeader(responsePackets, ref packetHeader)) 
			{
				if (packetHeader.DataLength >= maxBufferLength) 
				{
					_logger( "**** POSSIBLE BUFFER UNDERRUN EVENT " + ", " + packetHeader.DataLength + " ****" );
				}
				var typeTag = new string(packetHeader.TypeTag);
				// ToDo: the second comparison is redundant with the called func. Added it here to skip a function call. Might want to change the order later.
				if (String.Compare(typeTag, EmotibitPacket.TypeTag.THERMOPILE, StringComparison.Ordinal) == 0)
				{
					// Add stream to plot if data detected.
				}
				if (String.Compare(typeTag, EmotibitPacket.TypeTag.TEMPERATURE_1, StringComparison.Ordinal) == 0)
				{
					// Add stream to plot if data detected.
				}
				if (String.Compare(typeTag, EmotibitPacket.TypeTag.REQUEST_DATA, StringComparison.Ordinal) == 0)
				{
					return;
				}

				SensorData.Clear();
				EmotibitHeader header = new EmotibitHeader();
				header.DataLength = packetHeader.DataLength;
				header.TypeTag = packetHeader.TypeTag;
				header.DataReliability = packetHeader.DataReliability;
				header.Timestamp = packetHeader.Timestamp;
				header.ProtocolVersion = packetHeader.ProtocolVersion;
				header.PacketNumber = packetHeader.PacketNumber;
				
				for (int n = EmotibitPacket.headerLength; n < responsePackets.Count; n++) 
				{
					try
					{
						SensorData.Add( float.Parse(responsePackets[n], CultureInfo.InvariantCulture) );
					}
					catch (Exception e)
					{
						// ignored
					}
				}
				_latestSensorsDataFrame[typeTag] = new List<float>(SensorData);

				foreach (var sensorsDataFrame in _latestSensorsDataFrame)
				{
					if (OnDataReceived != null && sensorsDataFrame.Value.Count > 0)
					{
						OnDataReceived.Invoke(sensorsDataFrame.Key, sensorsDataFrame.Value, header);
					}
				}
			}
		}

		private void TrimArrayToLastN(List<dynamic> array, int n)
		{
			if (array.Count > n)
			{
				array.RemoveRange(0, array.Count - n);
			}
		}

		private EmotibitInfo[] GetDiscoveredEmotibits()
		{
			_discoveredEmotibitsMutex.WaitOne();
			var outEmotibits = _discoveredEmotibits.Values.ToArray();
			_discoveredEmotibitsMutex.ReleaseMutex();
			return outEmotibits;
		}


		/// <summary>
		/// Placeholder function to load settings from a file at a given path using the provided deserializer.
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="deserializer"></param>
		private void LoadSettings(string filePath, Func<string,EmotibitConfig> deserializer)
		{
			string dateString = DateTime.UtcNow.ToString();
			
			string fileName = "/Config/emotibit.json";
			
			filePath += fileName;
			
			if (GetWifiHostSettings().DebugLogs)
			{
				_logger("Path for emotibit config " + filePath);
			}

			if (File.Exists(filePath))
			{
				
				string fileContent = File.ReadAllText(filePath);
				
				if (fileContent.Length > 0)
				{
					EmotibitConfig emotibitConfig;

					emotibitConfig = deserializer(fileContent); //JsonUtility.FromJson<EmotibitConfig>(fileContent);
					if (emotibitConfig != null)
					{
						if (GetWifiHostSettings().DebugLogs)
						{
							_logger("Loaded Emotibit Settings Successfully");
						}
						SetWifiHostSettings(emotibitConfig);
					}
					else
					{
						if (GetWifiHostSettings().DebugLogs)
						{
							_logger("Failed to Load Emotibit Settings");
						}
					}
				}
			}
		} 

		/// <summary>
		/// Function used to stop, join and destroy the worker threads.
		/// </summary>
		public void Destroy()
		{
			// Gracefully stop worker threads and dispose resources
			try
			{
				_stopDataThread = true;
				_stopAdvertisingThread = true;
				if (_dataThread != null)
				{
					try
					{
						if (_dataThread.IsAlive)
						{
							_dataThread.Join(TimeSpan.FromSeconds(2));
						}
					}
					catch { /* ignored */ }
				}
				if (_advertisingThread != null)
				{
					try
					{
						if (_advertisingThread.IsAlive)
						{
							_advertisingThread.Join(TimeSpan.FromSeconds(2));
						}
					}
					catch { /* ignored */ }
				}
			}
			finally
			{
				// Close and dispose sockets regardless of current connection state
				try { _advertisingCxn?.Shutdown(SocketShutdown.Both); } catch { }
				try { _advertisingCxn?.Close(); } catch { }
				try { _advertisingCxn?.Dispose(); } catch { }
				try { _dataCxn?.Shutdown(SocketShutdown.Both); } catch { }
				try { _dataCxn?.Close(); } catch { }
				try { _dataCxn?.Dispose(); } catch { }
				try { _controlCxn?.Shutdown(SocketShutdown.Both); } catch { }
				try { _controlCxn?.Close(); } catch { }
				try { _controlCxn?.Dispose(); } catch { }
				// Dispose mutexes to release OS handles
				try { _controlCxnMutex?.Dispose(); } catch { }
				try { _dataCxnMutex?.Dispose(); } catch { }
				try { _discoveredEmotibitsMutex?.Dispose(); } catch { }
				try { _packetsMutex?.Dispose(); } catch { }
				_emotibitsFound = false;
				Reset();
			}
		}

		/// <summary>
		/// Entry point for setting up the threads and sockets
		/// </summary>
		/// <returns></returns>
		private short Begin()
		{
			_advertisingPort = EmotibitComms.WIFI_ADVERTISING_PORT;
			GetAvailableNetworks();
			if (_availableNetworks.Count == 0) {
				if (_wifiHostSettings.DebugLogs)
				{
					_logger("check if network adapters are enabled");
				}
				return Fail;
			}
			
			// Create the UDP socket
			_advertisingCxn = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_advertisingCxn.Blocking = false;
			_advertisingCxn.EnableBroadcast = true;
			_advertisingCxn.ReceiveBufferSize = (int)Math.Pow(2, 10);
			_advertisingCxn.Bind(new IPEndPoint(IPAddress.Any, 0));
			
			//advertisingCxn = FUdpSocketBuilder(TEXT("advertisingCxn")).AsNonBlocking().WithBroadcast().WithReceiveBufferSize(FMath::Pow(2.,10.)).BoundToAddress(FIPv4Address::Any);
			
			_startDataCxn(EmotibitComms.WIFI_ADVERTISING_PORT + 1);

			_controlPort = (ushort)(_dataPort + 1);

			//controlCxn = FTcpSocketBuilder(TEXT("controlCxn")).AsNonBlocking().BoundToAddress(FIPv4Address::Any).BoundToPort(controlPort).Listening(8);
			
			_controlCxn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_controlCxn.Blocking = false;
			_controlCxn.ReceiveBufferSize = (int)Math.Pow(2, 10);
			_controlCxn.Bind(new IPEndPoint(IPAddress.Any, _controlPort));
			_controlCxn.Listen(8);
			//controlCxn.setMessageDelimiter(ofToString(EmotibitPacket::PACKET_DELIMITER_CSV));
			while (!_controlCxn.IsBound)
			{
				// Try to setup a controlPort until we find one that's available
				_controlPort += 2;
				try { _controlCxn?.Dispose(); } catch { }
				_controlCxn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_controlCxn.Blocking = false;
				_controlCxn.ReceiveBufferSize = (int)Math.Pow(2, 10);
				_controlCxn.Bind(new IPEndPoint(IPAddress.Any, _controlPort));
				_controlCxn.Listen(8);
			}

			if (_wifiHostSettings.DebugLogs)
			{
				_logger("EmotiBit data port: " + _dataPort);
				
				_logger("EmotiBit control port: " + _controlPort);
			}
			_advertisingPacketCounter = 0;
			_controlPacketCounter = 0;
			_connectedEmotibitIp = "";
			_isConnected = false;
			_isStartingConnection = false;
			_dataThread = new Thread(() =>
			{
				UpdateDataThread();
			});
			_dataThread.IsBackground = true;
			_dataThread.Start();
			
			_advertisingThread  = new Thread(() =>
			{
				ProcessAdvertisingThread();
			});
			_advertisingThread.IsBackground = true;
			_advertisingThread.Start();
			
			/*FEmotibitWorker* AdvertisingWorker = new FEmotibitWorker([this]() { processAdvertisingThread(); }, stopAdvertisingThread);
			dataThread = FRunnableThread::Create(AdvertisingWorker, TEXT("EmotibitAdvertisingThread"));

			FEmotibitWorker* DataWorker = new FEmotibitWorker([this]() { updateDataThread(); }, stopDataThread);
			advertisingThread = FRunnableThread::Create(DataWorker, TEXT("EmotibitDataThread"));*/
			
			return Success;
		}

		/// <summary>
		/// Checks if the provided IP address is in the provided list of IP networks.
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="networkList"></param>
		/// <returns></returns>
		private bool IsInNetworkList(string ipAddress, List<string> networkList) {
			bool output = false;
			string[] ipSplit;
			ipSplit = ipAddress.Split('.');
			foreach (string listIp in networkList) {
				string[] listIpSplit;
				listIpSplit = listIp.Split('.');
				bool partMatch = true;
				for (int n = 0; n < ipSplit.Length && n < listIpSplit.Length; n++) {
					if (listIpSplit[n].CompareTo("*") == 0 || listIpSplit[n].CompareTo(ipSplit[n]) == 0) {
						// partial match
						bool breakpoint = true;
					}
					else {
						partMatch = false;
						break;
					}
				}
				if (partMatch == true) {
					// found a match!
					output = true;
					break;
				}
			}
			return output;
		}

		private bool IsInNetworkExcludeList(string ipAddress) {
			bool output = IsInNetworkList(ipAddress, _wifiHostSettings.NetworkExcludeList);
			//cout << "Exclude " << ipAddress << ".* : " << out << endl;
			return output;
		}

		private bool IsInNetworkIncludeList(string ipAddress) {
			bool output =  IsInNetworkList(ipAddress, _wifiHostSettings.NetworkIncludeList);
			//cout << "Include " << ipAddress << ".* : " << out << endl;
			return output;
		}


		private void GetAvailableNetworks() {
			List<string> ips = new List<string>();
			var currentavailableNetworks = _availableNetworks;
			const int numTriesGetIP = 10;
			int tries = 0;
			
			while (ips.Count <= 0 && tries < numTriesGetIP)
			{
				ips = GetLocalIPs();
				tries++;
			}
			if (ips.Count > 0) {
				//get all available Networks
				for (int network = 0; network < ips.Count; network++)
				{
					string[] ipSplit;
					ipSplit = ips[network].Split('.');
					string tempNetwork = ipSplit[0] + "." + ipSplit[1] + "." + ipSplit[2];

					
					if (_availableNetworks.IndexOf(tempNetwork) == -1
						&& IsInNetworkIncludeList(tempNetwork) 
						&& !IsInNetworkExcludeList(tempNetwork)) {
						_availableNetworks.Add(tempNetwork);
					}
				}
			}
			if (_availableNetworks.Count != currentavailableNetworks.Count) { //print all Networks whenever new Networks are detected
				string allAvailableNetworks = "";
				for (int network = 0; network < _availableNetworks.Count; network++) {
					allAvailableNetworks += "[" + _availableNetworks[network] + ".*] ";
				}
				if (_wifiHostSettings.DebugLogs)
				{
					_logger("All Network(s): " + allAvailableNetworks);
				}
			}
		}

		private static int _hostId = -1;
		private static long _unicastLastCycleCount = -1;
		private static long _sendLastCycleCount = -1;

		/// <summary>
		/// Signals availability of a new Emotibit using either broadcast or unicast.
		/// </summary>
		private void SendAdvertising() {

			if (_hostId == -1)
			{
				_hostId = _wifiHostSettings.UnicastIpRangeLowerBound;
			}

			if (_unicastLastCycleCount == -1)
			{
				_unicastLastCycleCount = DateTime.Now.Ticks;
				_sendLastCycleCount = DateTime.Now.Ticks;
			}

			long sendCurrentCycleCount = DateTime.Now.Ticks;
			long sendDeltaCycles = sendCurrentCycleCount - _sendLastCycleCount;
			double sendDeltaMilliseconds = sendDeltaCycles/TimeSpan.TicksPerMillisecond;
			
			if (sendDeltaMilliseconds >= _wifiHostSettings.SendAdvertisingInterval)
			{
				// Periodically start a new advertising send
				_sendLastCycleCount = DateTime.Now.Ticks;
				_startNewSend = true;
				_sendInProgress = true;
			}

			if (_emotibitNetworks.Count > 0)
			{
				// only search all networks until an EmotiBit is found
				// ToDo: consider permitting EmotiBits on multiple networks
				_emotibitsFound = true;
			}

			if (!_emotibitsFound && _startNewSend)
			{
				GetAvailableNetworks(); // Check if new network appeared after oscilloscope was open (e.g. a mobile hotspot)
			}

			// **** Handle advertising sends ****
			// Handle broadcast advertising
			if (_wifiHostSettings.EnableBroadcast && _startNewSend && !_isStartingConnection) {
				string broadcastIp;
				if (_emotibitsFound)
				{
					broadcastIp = _emotibitNetworks[0] + "." + 255;
				}
				else
				{
					broadcastIp = _availableNetworks[_broadcastNetwork] + "." + 255;
				}
				if (_wifiHostSettings.DebugLogs)
				{
					_logger("Sending advertising broadcast: " + sendDeltaMilliseconds);
					_logger("Broadcast IP: " + broadcastIp);
				}

				_startNewSend = false;
				string packet = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.HELLO_EMOTIBIT, _advertisingPacketCounter++, "", 0);

				EndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(broadcastIp), _advertisingPort);
				
				/*auto remoteAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
				auto localAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
				localAddress->SetAnyAddress();
				bool bIsValid;
				remoteAddress->SetIp(*broadcastIp, bIsValid);
				remoteAddress->SetPort(advertisingPort);
				remoteAddress->SetBroadcastAddress();*/

				_advertisingCxn.EnableBroadcast = true;
				//advertisingCxn->Bind(*localAddress);
				//advertisingCxn->Connect(*remoteAddress);

				// Convert to UTF-8
				var message = Encoding.ASCII.GetBytes(packet);
				var size = message.Length;
				var sent = 0;

				
				
				sent = _advertisingCxn.SendTo(message, size, SocketFlags.None, remoteAddress);
				
				//advertisingCxn->SendTo(reinterpret_cast<const uint8*>(Convert.Get()), Convert.Length(), Sent, *remoteAddress);
				
				//advertisingCxn.Connect(broadcastIp.c_str(), advertisingPort);
				//advertisingCxn.Send(packet.c_str(), packet.length());

				if (!_emotibitsFound)
				{
					_broadcastNetwork++;
					if (_broadcastNetwork >= _availableNetworks.Count)
					{
						_broadcastNetwork = 0;
					}
				}

				// skip unicast when broadcast sent to avoid network spam
				return;
			}
			// Handle unicast advertising
			if (_wifiHostSettings.EnableUnicast && _sendInProgress)
			{
				var unicastCurrentCycleCount = DateTime.Now.Ticks;
				var unicastDeltaCycles = unicastCurrentCycleCount - _unicastLastCycleCount;
				double unicastDeltaMilliseconds = unicastDeltaCycles /TimeSpan.TicksPerMillisecond ;
				// Limit the rate of unicast sending
				if (unicastDeltaMilliseconds >= _wifiHostSettings.UnicastMinLoopDelay)
				{
					_unicastLastCycleCount = DateTime.Now.Ticks;
					if (_wifiHostSettings.DebugLogs)
					{
						//_logger("Sending advertising unicast: " + UnicastDeltaMilliseconds);
					}
					for (int i = 0; i < _wifiHostSettings.NUnicastIpsPerLoop; i++)
					{
						string unicastIp;

						if (_emotibitsFound)
						{
							unicastIp = _emotibitNetworks[0] + "." + _hostId;
						}
						else
						{
							unicastIp = _availableNetworks[_unicastNetwork] + "." + _hostId;
						}

						if (_wifiHostSettings.EnableUnicast && _sendInProgress)
						{
							if (_wifiHostSettings.DebugLogs)
							{
								_logger("Unicast IP: " + unicastIp);
							}
							string packet = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.HELLO_EMOTIBIT, _advertisingPacketCounter++, "", 0);

							var remoteAddress = new IPEndPoint(IPAddress.Parse(unicastIp), _advertisingPort);
							
							//advertisingCxn->Connect(*remoteAddress);

							// Convert to UTF-8
							var message = Encoding.ASCII.GetBytes(packet);
							var size = message.Length;
							var sent = 0;

							// Convert string to bytes
							//FTCHARToUTF8 Convert(*Message);
							//advertisingCxn->SetBroadcast(false);
							//advertisingCxn->SendTo(reinterpret_cast<const uint8*>(Convert.Get()), Convert.Length(), Sent, *remoteAddress);
							_advertisingCxn.EnableBroadcast = false;
							try
							{
								_advertisingCxn.SendTo(message, size, SocketFlags.None, remoteAddress);
							}
							catch (Exception e)
							{
								Console.WriteLine(e);
							}
							
							//advertisingCxn.SetEnableBroadcast(false);
							//advertisingCxn.Connect(unicastIp.c_str(), advertisingPort);
							//advertisingCxn.Send(packet.c_str(), packet.length());
						}

						// Iterate IP Address
						if (_hostId < _wifiHostSettings.UnicastIpRangeUpperBound)
						{
							_hostId++;
						}
						else
						{
							// Reached end of unicastIpRange
							_hostId = _wifiHostSettings.UnicastIpRangeLowerBound; // loop hostId back to beginning of range
							if (_emotibitsFound)
							{
								// finished a send of all IPs
								_sendInProgress = false;
								break;
							}
							else
							{
								_unicastNetwork++;
								if (_unicastNetwork >= _availableNetworks.Count)
								{
									// reached end of unicastIpRange for the last known network in list
									_sendInProgress = false;
									_unicastNetwork = 0;
									break;
								}
							}
						}
					}
				}
			}
		}

		private void UpdateAdvertisingIpList(string ip) {
			var currentEmotibitNetworks = _emotibitNetworks;
			var ipSplit = ip.Split('.');
			string networkAddr = ipSplit[0] + "." + ipSplit[1] + "." + ipSplit[2];

			if (_emotibitNetworks.Count == 0) { //assume emotibits are all on the same network
				_emotibitNetworks.Add(networkAddr);
			}

			//print all emotibit ip adrresses and/or Networks whenever new emotibits are detected
			if (_emotibitNetworks.Count != currentEmotibitNetworks.Count) {
				string allEmotibitNetworks = "";
				for (int network = 0; network < _emotibitNetworks.Count; network++) {
					allEmotibitNetworks += "[" + _emotibitNetworks[network] + ".*] ";
				}
				if (_wifiHostSettings.DebugLogs)
				{
					_logger("Emotibit Network(s): " + allEmotibitNetworks);
				}
			}
		}

		/// <summary>
		/// Advertising thread function loop to either advertise or attempt to connect to an Emotibit.
		/// </summary>
		private void ProcessAdvertisingThread()
		{
			bool foundEmotibit = false;
			while (!_stopAdvertisingThread)
			{
				List<string> infoPackets = new List<string>();
				var discoveredEmotibits = GetdiscoveredEmotibits();
				if (_emotibitsFound && _connectedEmotibitIdentifier != discoveredEmotibits.First().Key && !foundEmotibit)
				{
					foundEmotibit = true;
					Connect(discoveredEmotibits.First().Key);
				}
				else
				{
					ProcessAdvertising(ref infoPackets);
					try
					{
						if (_controlCxn != null && _controlCxn.IsBound && _controlCxn.Available > 0)
						{
							int pendingDataSize = 50;
							byte[] tempBufer = new byte[pendingDataSize];
							EndPoint source = new IPEndPoint(IPAddress.Any, 0);
							int bytesRead =
								_controlCxn.ReceiveFrom(tempBufer, pendingDataSize, SocketFlags.None, ref source);
							/*bool bOk = controlCxn->Wait(ESocketWaitConditions::WaitForRead, FTimespan::FromMilliseconds(50));
							if (bOk)
							{
								TArray<uint8> TempBuffer;
								TempBuffer.SetNumUninitialized(PendingDataSize);
								auto Source = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
								int32 BytesRead = 0;
								controlCxn->RecvFrom(TempBuffer.GetData(), TempBuffer.Num(), BytesRead, *Source);
							}*/
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
				
				// ToDo: Handle info packets with mode change information
				ThreadSleepFor(_wifiHostSettings.AdvertisingThreadSleep);
			}
		}

		private static long _lastCycleCount = -1;
		private const int MaxSize = 32768;
		private static readonly byte[] UDPMessage = new byte[MaxSize];
		private static long _pingLastCycleCount = -1;

		private static long _cnxLastCycleCount =  -1;

		/// <summary>
		/// Function to process advertising response packets. This includes replying to HELLO_HOST and PING/PONG packets as well as dealing with connection logic
		/// </summary>
		/// <param name="infoPackets"></param>
		/// <returns></returns>
		private int ProcessAdvertising(ref List<string> infoPackets)
		{
			SendAdvertising();

			if (_lastCycleCount == -1)
			{
				_lastCycleCount = DateTime.Now.Ticks;
			}

			long currentCycleCount = DateTime.Now.Ticks;
			long deltaCycles = currentCycleCount - _lastCycleCount;
			double deltaMilliseconds = deltaCycles / TimeSpan.TicksPerMillisecond;

			if (deltaMilliseconds >= _wifiHostSettings.CheckAdvertisingInterval)
			{
				_lastCycleCount = DateTime.Now.Ticks;
				if (_wifiHostSettings.DebugLogs)
				{
					_logger("checkAdvertising: " + deltaMilliseconds);
				}
				//static uint8* udpMessage = UdpMessage.GetData();
				int msgSize = 0;
				int dataSize;
				int ip;
				int port;
				string ipStr;
				EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
				//TSharedRef<FInternetAddr> Sender = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
				//Sender->SetAnyAddress();


				try
				{
					msgSize = _advertisingCxn.Available > 0 ? _advertisingCxn.ReceiveFrom(UDPMessage, ref sender) : 0;
				}
				catch (SocketException e)
				{
					msgSize = 0;
				}

				// int msgSize= advertisingCxn.Receive(udpMessage, maxSize);
				if (msgSize > 0)
				{
					ipStr = ((IPEndPoint)sender).Address.ToString();
					port =  ((IPEndPoint)sender).Port;
					if (_wifiHostSettings.DebugLogs)
					{
						_logger("Received From: " + ipStr +":"+ port );
					}

					
					
					string message = Encoding.ASCII.GetString(UDPMessage, 0, msgSize); //FString(ANSI_TO_TCHAR(reinterpret_cast<const char*>(UdpMessage)));
					if (_wifiHostSettings.DebugLogs)
					{
						_logger("Received: " + message);
					}
					string[] packets;

					char c = EmotibitPacket.PACKET_DELIMITER_CSV;
					//char[] buffer = { c, '\0' };
				
					packets = message.Split(c);

					foreach (string packet in packets)
					{
						EmotibitHeader header;
						int dataStartChar = EmotibitPacket.getHeader(packet, out header);
						if (dataStartChar > 0)
						{
							var typeTag = new string(header.TypeTag);
							if (typeTag.CompareTo(EmotibitPacket.TypeTag.HELLO_HOST) == 0)
							{
								// HELLO_HOST
								string value;
								string emotibitDeviceId;
								int valuePos = EmotibitPacket.getPacketKeyedValue(packet, EmotibitPacket.PayloadLabel.DATA_PORT, out value, dataStartChar);
								if (valuePos > -1)
								{
									UpdateAdvertisingIpList(ipStr);
									if (_wifiHostSettings.DebugLogs)
									{
										_logger("EmotiBit ip: " + ipStr +":"+ port);
									}
									int deviceIdPos = -1;
									deviceIdPos = EmotibitPacket.getPacketKeyedValue(packet, EmotibitPacket.PayloadLabel.DEVICE_ID, out emotibitDeviceId, dataStartChar);
									if (deviceIdPos > -1)
									{
										// found EmotiBitSrNum in HELLO_HOST message
										// do nothing. emotibitDeviceid already updated.
										if (_wifiHostSettings.DebugLogs)
										{
											_logger("EmotiBit DeviceId: " + emotibitDeviceId);
										}
									}
									else
									{
										emotibitDeviceId = ipStr;
										if (_wifiHostSettings.DebugLogs)
										{
											_logger("EmotiBit DeviceId: DeviceId not available. using IP address as identifier");
										}
										// Add ip address to our list
									}
									_discoveredEmotibitsMutex.WaitOne();
									
									if (_discoveredEmotibits.ContainsKey(emotibitDeviceId))
									{
										// if it's not a new ip address, update the status
										_discoveredEmotibits[emotibitDeviceId] = new EmotibitInfo(ipStr,  int.Parse(value) == EmotibitComms.EMOTIBIT_AVAILABLE, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) );
									}
									else _discoveredEmotibits.Add(emotibitDeviceId, new EmotibitInfo(ipStr, int.Parse(value) == EmotibitComms.EMOTIBIT_AVAILABLE, (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) ));
									
									_discoveredEmotibitsMutex.ReleaseMutex();
								}
							}
							else if (typeTag.CompareTo(EmotibitPacket.TypeTag.PONG) == 0)
							{
								// PONG
								if (ipStr.CompareTo(_connectedEmotibitIp) == 0)
								{
									string value;
									int valuePos = EmotibitPacket.getPacketKeyedValue(packet, EmotibitPacket.PayloadLabel.DATA_PORT, out value, dataStartChar);
									if (valuePos > -1 && int.Parse(value) == _dataPort)
									{
										// Establish / maintain connected status
										if (_isStartingConnection)
										{
											//flushData();
											_isConnected = true;
											_isStartingConnection = false;
											//dataCxn.Create();
										}
										if (_isConnected)
										{
											_connectionTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
										}
									}
								}
							}
							else
							{
								infoPackets.Add(packet);
							}
						}
					}
				}
			}

			if (_isConnected)
			{
				// If we're connected, periodically send a PING to EmotiBit
				if (_pingLastCycleCount == -1)
				{
					_pingLastCycleCount = DateTime.Now.Ticks;
				}
				long pingCurrentCycleCount = DateTime.Now.Ticks;
				long pingDeltaCycles = pingCurrentCycleCount - _pingLastCycleCount;
				double pingDeltaSeconds = pingDeltaCycles / TimeSpan.TicksPerMillisecond;
				double pingDeltaMilliseconds = pingDeltaSeconds * 1000.0;
				if (pingDeltaMilliseconds > _pingInterval)
				{
					_pingLastCycleCount = pingCurrentCycleCount;

					List<string> payload =  new List<string>
					{
						EmotibitPacket.PayloadLabel.DATA_PORT,
						_dataPort.ToString()
					};
					string packet = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.PING, _advertisingPacketCounter++, payload);

					if (_wifiHostSettings.DebugLogs)
					{
						_logger("Sent: "+ packet);
					}
					/*var remoteAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
					bool bIsValid;
					remoteAddress->SetIp(*FString(connectedEmotibitIp.c_str()), bIsValid);
					remoteAddress->SetPort(advertisingPort);*/

					//advertisingCxn->Connect(remoteAddress.Get());

					EndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(_connectedEmotibitIp),
						_advertisingPort);
					
					_advertisingCxn.Connect(remoteAddress);

					// Convert to UTF-8
					var message = Encoding.ASCII.GetBytes(packet);
					int size = message.Length;
					int sent = 0;

					// Convert string to bytes
					//FTCHARToUTF8 Convert(*Message);
					_advertisingCxn.EnableBroadcast = false; // ->SetBroadcast(false);
					//advertisingCxn->Send(reinterpret_cast<const uint8*>(Convert.Get()), Convert.Length(), Sent);
					_advertisingCxn.Send(message, size, SocketFlags.None);
					//advertisingCxn.Connect(connectedEmotibitIp.c_str(), advertisingPort);
					//advertisingCxn.SetEnableBroadcast(false);
					//advertisingCxn.Send(packet.c_str(), packet.length());
				}
			}

			// Handle connecting to EmotiBit
			if (_isStartingConnection) {
				// Send connect messages periodically
				if (_cnxLastCycleCount == -1)
				{
					_cnxLastCycleCount = DateTime.Now.Ticks;
				}

				long cnxCurrentCycleCount = DateTime.Now.Ticks;
				long cnxDeltaCycles = cnxCurrentCycleCount - _cnxLastCycleCount;
				double cnxDeltaMilliseconds = cnxDeltaCycles / TimeSpan.TicksPerMillisecond;
				if (cnxDeltaMilliseconds > _startCxnInterval || cnxDeltaMilliseconds <= 1)
				{
					_cnxLastCycleCount = cnxCurrentCycleCount;

					// Send a connect message to the selected EmotiBit
					List<string> payload = new List<string>();
					payload.Add(EmotibitPacket.PayloadLabel.CONTROL_PORT);
					payload.Add(_controlPort.ToString());
					payload.Add(EmotibitPacket.PayloadLabel.DATA_PORT);
					payload.Add(_dataPort.ToString());
					string packet = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.EMOTIBIT_CONNECT, _advertisingPacketCounter++, payload);

					EndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(_connectedEmotibitIp), _advertisingPort);
					
					/*bool bIsValid;
					remoteAddress->SetIp(*FString(connectedEmotibitIp.c_str()), bIsValid);
					remoteAddress->SetPort(advertisingPort);

					advertisingCxn->Connect(remoteAddress.Get());*/
					
					_advertisingCxn.Connect(remoteAddress);

					// Convert to UTF-8
					var message = Encoding.ASCII.GetBytes(packet);
					int size = message.Length;
					int sent = 0;

					// Convert string to bytes
					//FTCHARToUTF8 Convert(*Message);
					_advertisingCxn.EnableBroadcast = false;
					//advertisingCxn->SetBroadcast(false);
					sent = _advertisingCxn.Send(message);
					//advertisingCxn->Send(reinterpret_cast<const uint8*>(Convert.Get()), Convert.Length(), Sent);
					
					if (_wifiHostSettings.DebugLogs)
					{
						_logger("Sent: " + packet);
					}
					//advertisingCxn.Connect(connectedEmotibitIp.c_str(), advertisingPort);
					//advertisingCxn.SetEnableBroadcast(false);
					//advertisingCxn.Send(packet.c_str(), packet.length());
					
					
				}

				// Timeout starting connection if no response is received
				if (false)//(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - Single.Epsilon > startCxnTimeout)
				{
					_isStartingConnection = false;
					_connectedEmotibitIp = "";
					_connectedEmotibitIdentifier = "";
				}
			}

			// Check to see if connection has timed out
			if (_isConnected)
			{
				if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _connectionTimer > _connectionTimeout)
				{
					//disconnect();
				}
			}

			// Check to see if EmotiBit availability is stale or needs purging
			_discoveredEmotibitsMutex.WaitOne();
			foreach (var key in _discoveredEmotibits.Keys)
			{
				var discoveredEmotibit = _discoveredEmotibits[key];
				if(false)//if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - discoveredEmotibit.LastSeen > availabilityTimeout)
				{
					discoveredEmotibit.BIsAvailable = false;
				}

				//if (FPlatformTime::ToMilliseconds64(FPlatformTime::Seconds()) - it->second.lastSeen > ipPurgeTimeout)
				//{
				//	_emotibitIps.erase(it);
				//}
				//else
				//{
				//	it++;
				//}
			}
			_discoveredEmotibitsMutex.ReleaseMutex();
			infoPackets = null;
			return Success;
		}

		/// <summary>
		/// Unused function to send control packets to EmotiBit. These can be used to send the Emotibit to a specific mode.
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		private short SendControl(string packet)
		{
			EmotibitWifiHost._controlCxnMutex.WaitOne();
			string ipString;
			IPEndPoint remoteAddress = (IPEndPoint)_controlCxn.RemoteEndPoint;
			
			ipString = remoteAddress.Address.ToString();
			
			if (_connectedEmotibitIp.Equals(ipString))
			{
				int bytesSent = _controlCxn.Send(Encoding.ASCII.GetBytes(packet));
				//controlCxn->Send(reinterpret_cast<const uint8*>(packet.data()), packet.size(),BytesSent);
			}
			
			_controlCxnMutex.ReleaseMutex();

			return Success;
		}

		// ToDo: Implement readControl()
		//uint8_t EmotiBitWiFiHost::readControl(string& packet)
		//{
		//	if (_isConnected) {
		//
		//		// for each connected client lets get the data being sent and lets print it to the screen
		//		for (unsigned int i = 0; i < (unsigned int)controlCxn.getLastID(); i++) {
		//
		//			if (!controlCxn.isClientConnected(i)) continue;
		//
		//			// get the ip and port of the client
		//			string port = ofToString(controlCxn.getClientPort(i));
		//			string ip = controlCxn.getClientIP(i);
		//
		//			if (ip.compare(connectedEmotibitIp) != 0) continue;
		//			//string info = "client " + ofToString(i) + " -connected from " + ip + " on port: " + port;
		//			//cout << info << endl;
		//
		//			packet = "";
		//			string tmp;
		//			do {
		//				packet += tmp;
		//				tmp = controlCxn.receive(i);
		//			} while (tmp != EmotibitPacket::PACKET_DELIMITER_CSV);
		//
		//			// if there was a message set it to the corresponding client
		//			if (str.length() > 0) {
		//				cout << "Message: " << str << endl;
		//			}
		//
		//			cout << "Sending: m" << endl;
		//			messageConn.send(i, "m");
		//		}
		//	}
		//	return SUCCESS;
		//}
		private const int UDPMessageMaxSize = 32768;
		private static readonly byte[] UDPReadMessage = new byte[UDPMessageMaxSize];

		/// <summary>
		/// Reads from the provided UDP socket and uses a fixed size buffer to store the data.
		/// </summary>
		/// <param name="udp"></param>
		/// <param name="message"></param>
		/// <param name="ipFilter"></param>
		private void ReadUdp(Socket udp, out string message, string ipFilter)
		{
			if (udp.Available == 0)
			{
				message = "";
				return;
			}
			int msgSize = 0;
			string ip;
			int port;
			string ipStr;
			EndPoint sender = new IPEndPoint(IPAddress.Any, 0); // ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
			//udp.RecvFrom(udpMessage,maxSize, msgSize, *Sender );
			msgSize = udp.ReceiveFrom(UDPReadMessage, SocketFlags.None, ref sender);
			if (msgSize > 0)
			{
				//msgSize = udp.Receive(udpMessage, maxSize);
				ipStr = ((IPEndPoint) sender).Address.ToString();
				
				if (ipFilter.Length == 0 || ipStr.CompareTo(ipFilter) == 0) // && portFilter > -1 && port == portFilter)
				{
					message = Encoding.ASCII.GetString(UDPReadMessage,0,msgSize); 
					// string (reinterpret_cast<const char*>(udpMessage),msgSize);
				}
				else
				{
					message = "";
				}
				Array.Clear(UDPReadMessage, 0, UDPReadMessage.Length);
			}
			else
			{
				message = "";
			}
		}

		/// <summary>
		/// Funcion to read sensor data from the EmotiBit data socket
		/// </summary>
		private async Task UpdateData()
		{
			string message;
			_dataCxnMutex.WaitOne();
			ReadUdp(_dataCxn, out message, _connectedEmotibitIp);
			_dataCxnMutex.ReleaseMutex();

			/*if (!_isConnected)
			{
				// flush the data if we're not connected
				await flushData();
				return;
			}*/

			if (message.Length > 0)
			{
				string packet;
				EmotibitHeader header;
				int startChar = 0;
				int endChar;
				do
				{
					
					endChar = message.IndexOf(EmotibitPacket.PACKET_DELIMITER_CSV); 
					// message.find_first_of(EmotibitPacket::PACKET_DELIMITER_CSV, startChar);
					if (endChar == -1)
					{
						if (_wifiHostSettings.DebugLogs)
						{
							_logger("**** MALFORMED MESSAGE **** : no packet delimiter found");
						}
					}
					else
					{
						if (endChar == startChar)
						{
							if (_wifiHostSettings.DebugLogs)
							{
								_logger("**** EMPTY MESSAGE **** ");
							}
						}
						else
						{
							packet = message.Substring(startChar, endChar - startChar);	// extract packet

							int dataStartChar = EmotibitPacket.getHeader(packet, out header);	// read header
							if (dataStartChar == EmotibitPacket.MALFORMED_HEADER)
							{
								if (_wifiHostSettings.DebugLogs)
								{
									_logger("**** MALFORMED PACKET **** : no header data found");
								}
							}
							else
							{
								// We got a well-formed packet header
								if (startChar == 0)
								{
									// This is the first packet in the message
									if (_isConnected)
									{
										// Connect a channel to handle time syncing
										_dataCxnMutex.WaitOne();
										EndPoint remoteAddress = new IPEndPoint(IPAddress.Parse(_connectedEmotibitIp),
											_dataPort); // dataCxn.RemoteEndPoint; 
										//	ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
										//dataCxn.RemoteEndPoint ->GetPeerAddress(*RemoteAddress);
										string ipString = ((IPEndPoint)remoteAddress).Address.ToString(); //->ToString(false);
										int port = ((IPEndPoint)remoteAddress).Port;

										if (port != _sendDataPort)
										{
											_sendDataPort = port;
											_dataCxn.Connect(remoteAddress);
											_advertisingCxn.EnableBroadcast = false;
										}
										_dataCxnMutex.ReleaseMutex();
									}
									if (header.PacketNumber == _receivedDataPacketNumber)
									{
										// THIS DOESN'T WORK YET
										// Skip duplicates packets (e.g. from multi-send protocols)
										// Note this assumes the whole message is a duplicate
										//continue;
									}
									else
									{
										// Keep track of packetNumbers we've seen
										_receivedDataPacketNumber = header.PacketNumber;
									}
								}

								var typeTag = new string(header.TypeTag);
								if (typeTag.CompareTo(EmotibitPacket.TypeTag.REQUEST_DATA) == 0)
								{
									// Process data requests
									ProcessRequestData(packet, dataStartChar);
								}
								_packetsMutex.WaitOne();
								_dataPackets.Add(packet);
								_packetsMutex.ReleaseMutex();
							}
						}
					}
					startChar = endChar + 1;
				} while (endChar != -1 && startChar < message.Length);	// until all packet delimiters are processed
			}
		}

		private void UpdateDataThread()
		{
			while (!_stopDataThread)
			{
				UpdateData();
				ThreadSleepFor(_wifiHostSettings.DataThreadSleep);
			}
		}

		private void ThreadSleepFor(int sleepMicros)
		{
			if (sleepMicros < 0)
			{
				//	do nothing, not even yield
				//	WARNING: high spinlock potential
			}
			else if (sleepMicros == 0)
			{
				//this_thread::yield();
				Thread.Yield();
				//FPlatformProcess::Yield();
			}
			else
			{
				// Convert microseconds to milliseconds for Thread.Sleep (milliseconds)
				// Note: sub-millisecond values will effectively yield the thread.
				int sleepMilliseconds = (int)((sleepMicros) / 1000.0f);
				Thread.Sleep(sleepMilliseconds);
			}
		}

		/// <summary>
		/// Requests multiple data packets from the EmotiBit and ACKs the request.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="dataStartChar"></param>
		private void ProcessRequestData(string packet, int dataStartChar)
		{
			// Request Data
			string element;
			string outPacket;
			do
			{
				// Parse through requested packet elements and data
				dataStartChar = EmotibitPacket.getPacketElement(packet, out element, dataStartChar);

				if (element.CompareTo(EmotibitPacket.TypeTag.TIMESTAMP_LOCAL) == 0)
				{
					outPacket = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.TIMESTAMP_LOCAL, _dataPacketCounter++, OfGetTimestampString(EmotibitPacket.TIMESTAMP_STRING_FORMAT), 1);
					SendData(outPacket);
				}
				if (element.CompareTo(EmotibitPacket.TypeTag.TIMESTAMP_UTC) == 0)
				{
					// ToDo: implement UTC timestamp
				}
				//if (lsl.isConnected()) {
				//	double lsltime = lsl::local_clock();
				//	sendEmotibitPacket(EmotibitPacket.TypeTag.TIMESTAMP_CROSS_TIME, ofToString(EmotibitPacket.TypeTag.TIMESTAMP_LOCAL) + "," + ofGetTimestampString(EmotibitPacket::TIMESTAMP_STRING_FORMAT) + ",LC," + ofToString(lsltime, 7));
				//	//cout << EmotibitPacket.TypeTag.TIMESTAMP_CROSS_TIME << "," << ofToString(EmotibitPacket.TypeTag.TIMESTAMP_LOCAL) + "," + ofGetTimestampString(EmotibitPacket::TIMESTAMP_STRING_FORMAT) + ",LC," + ofToString(lsltime, 7) << endl;
				//}
				//sendEmotibitPacket(EmotibitPacket.TypeTag.ACK, ofToString(header.packetNumber) + ',' + header.TypeTag, 2);
				////cout << EmotibitPacket::TypeTag::REQUEST_DATA << header.packetNumber << endl;
			} while (dataStartChar > 0);
			EmotibitHeader header;
			EmotibitPacket.getHeader(packet, out header);
			List<string> payload = new List<string>();
			payload.Add(header.PacketNumber.ToString());
			payload.Add(new string(header.TypeTag));
			outPacket = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.ACK, _dataPacketCounter++, payload);
			SendData(outPacket);
		}

		private short SendData(string packet)
		{
			if (_isConnected)
			{
				_dataCxnMutex.WaitOne();
				int bytesSent;
				bytesSent = _dataCxn.Send(Encoding.ASCII.GetBytes(packet));
				//dataCxn->Send(reinterpret_cast<const uint8*> (packet.data()), packet.size(),bytesSent);
				_dataCxnMutex.ReleaseMutex();
				return Success;
			}
			else
			{
				return Fail;
			}
		}

		private void ReadData(ref List<string> packets)
		{
			if (_dataPackets.Count == 0)
			{
				packets = null;
				return;
			}
			_packetsMutex.WaitOne();
			packets = _dataPackets.ConvertAll(p => p) ;
			_dataPackets.Clear();
			_packetsMutex.ReleaseMutex();
		}

		/// <summary>
		/// Gracefully disconnects from the EmotiBit.
		/// </summary>
		/// <returns></returns>
		private short Disconnect()
		{
			if (_isConnected)
			{
				_controlCxnMutex.WaitOne();
				string packet = EmotibitPacket.createPacket(EmotibitPacket.TypeTag.EMOTIBIT_DISCONNECT, _controlPacketCounter++, "", 0);

				//auto peerAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
				EndPoint remoteAddress = _controlCxn.RemoteEndPoint;
				
				//controlCxn->GetPeerAddress(*peerAddress);
				//FString fip = peerAddress->ToString(false);
				string ip = ((IPEndPoint) remoteAddress).Address.ToString();
				//for (int i = controlCxn.getLastID() - 1; i >= 0; i--) {
				//string ip = controlCxn.getClientIP(i);
				if (ip.CompareTo(_connectedEmotibitIp) == 0)
				{
					int bytesSent = _controlCxn.Send(Encoding.ASCII.GetBytes(packet));
					//controlCxn->Send(reinterpret_cast<const uint8*>(packet.data()), packet.size(), bytesSent);
				}
				//}
				_controlCxnMutex.ReleaseMutex();
				FlushData();
				//dataCxn.Close();
				//_startDataCxn(controlPort + 1);
				_connectedEmotibitIp = "";
				_connectedEmotibitIdentifier = "";
				_isConnected = false;
				_isStartingConnection = false;
			}

			return Success;
		}

		private short _startDataCxn(int dataPort)
		{
			_dataPort = dataPort;
			/*dataCxn = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateSocket(
			NAME_DGram,         // Datagram = UDP
			TEXT("Data Connection"), // Optional debug name
			true               // bForceUDP (false is fine here)
			);*/
			_dataCxn = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_dataCxn.Blocking = false;
			//dataCxn.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
			_dataCxn.ReceiveBufferSize = (int)Math.Pow(2, 10);
			_dataCxn.Bind(new IPEndPoint(IPAddress.Any, dataPort));
				
			//FUdpSocketBuilder(TEXT("dataCxn")).AsNonBlocking().WithBroadcast().WithReceiveBufferSize(FMath::Pow(2.,10.)).BoundToAddress(FIPv4Address::Any).BoundToPort(_dataPort);
			
			//dataCxn->SetReuseAddr(false);// SetReuseAddress(false);
			//dataCxn.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);

			//var address = new IPEndPoint(IPAddress.Any, _dataPort);
			//ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
			//address->SetPort(_dataPort);
			//address->SetAnyAddress();
			/*while (!dataCxn->Bind(*address))
			{
				// Try to bind _dataPort until we find one that's available
				_dataPort += 2;
				address->SetAnyAddress();
				address->SetPort(_dataPort);
				if (EmotibitConfig.DebugLogs)
			{
		    UE_LOG(LogTemp, Log, TEXT("Trying data port: %i"), _dataPort);
			}*/
			//dataCxn.SetEnableBroadcast(false);
			//dataCxn->SetNonBlocking(true);
			int newSize;
			_dataCxn.ReceiveBufferSize = (int)Math.Pow(2, 15);
			//dataCxn->SetReceiveBufferSize(pow(2, 15),newSize);

			if (_wifiHostSettings.DebugLogs)
			{
				_logger("dataCxn GetReceiveBufferSize: " + _dataCxn.ReceiveBufferSize);
			}
			//ofLogNotice() << "dataCxn GetMaxMsgSize: " << dataCxn.GetMaxMsgSize();
			//ofLogNotice() << "dataCxn GetReceiveBufferSize: " << dataCxn.GetReceiveBufferSize();
			//ofLogNotice() << "dataCxn GetTimeoutReceive: " << dataCxn.GetTimeoutReceive();

			return Success;
		}

		public static Task<int> ReceiveAsync(Socket socket, byte[] buffer)
		{
			var tcs = new TaskCompletionSource<int>();

			var args = new SocketAsyncEventArgs();
			args.SetBuffer(buffer, 0, buffer.Length);

			args.Completed += (sender, e) =>
			{
				if (e.SocketError == SocketError.Success)
					tcs.SetResult(e.BytesTransferred);
				else
					tcs.SetException(new SocketException((int)e.SocketError));
			};

			if (!socket.ReceiveAsync(args))
			{
				// Completed synchronously
				if (args.SocketError == SocketError.Success)
					tcs.SetResult(args.BytesTransferred);
				else
					tcs.SetException(new SocketException((int)args.SocketError));
			}

			return tcs.Task;
		}

		private async Task<short> FlushData()
		{
			const int maxSize = 32768;
			byte[] udpMessage = new byte[maxSize];
			int bytesRead = 1;
			while (bytesRead > 0)
			{
				_dataCxnMutex.WaitOne();
				bytesRead = await ReceiveAsync(_dataCxn, udpMessage);
				//bytesRead = dataCxn.Receive(udpMessage);
				//dataCxn->Recv(reinterpret_cast<uint8*> (udpMessage), maxSize,bytesRead);
				_dataCxnMutex.ReleaseMutex();
			};
			//dataCxn.Close();
			//dataCxn.Create();
			return Success;
		}

		// Connecting is done asynchronously because attempts are repeated over UDP until connected
		private short Connect(string deviceId)
		{
			if (!_isStartingConnection && !_isConnected)
			{
				_discoveredEmotibitsMutex.WaitOne();
				string ip = _discoveredEmotibits[deviceId].Ip;
				bool isAvailable = _discoveredEmotibits[deviceId].BIsAvailable;
				_discoveredEmotibitsMutex.ReleaseMutex();
				
				if (ip.CompareTo("") != 0 && isAvailable)	// If the ip is on our list and available
				{
					_connectedEmotibitIp =  ip;
					_connectedEmotibitIdentifier = deviceId;
					_isStartingConnection = true;
					_startCxnAbortTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; 
					// FPlatformTime::ToMilliseconds64(FPlatformTime::Seconds());
				}
				else
				{
					if (_wifiHostSettings.DebugLogs)
					{
						_logger("EmotiBit %s not found" + ip);
					}
				}
			}
			return Success;
		}

		private Dictionary<string, EmotibitInfo> GetdiscoveredEmotibits()
		{
			_discoveredEmotibitsMutex.WaitOne();
			var output = _discoveredEmotibits;
			_discoveredEmotibitsMutex.ReleaseMutex();

			return output;
		}

		/// <summary>
		/// Returns a DateTime.Now based timestamp with the given format
		/// </summary>
		/// <param name="timestampFormat"></param>
		/// <returns></returns>
		private string OfGetTimestampString(string timestampFormat)
		{
			string ret = DateTime.Now.ToString(timestampFormat);
			return ret;
		}

		private List<string> GetLocalIPs()
		{
			return _wifiHostSettings.LocalIPs;
		}

		private bool IsConnected()
		{
			return _isConnected;
		}

		private void SetWifiHostSettings(EmotibitConfig settings)
		{
			_wifiHostSettings = settings;
		}

		private EmotibitConfig GetWifiHostSettings()
		{
			return _wifiHostSettings;
		}
    }
}