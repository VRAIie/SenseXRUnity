using System;
using System.Collections.Generic;

namespace SenseXR.Core.Demo.OSC.Emotibit.Utils
{
    public class EmotibitPacket
    {
	    public class TypeTag
	    {
		    // EmotiBit Data TagTypes
		    public static readonly string EDA = "EA";
		    public static readonly string EDL = "EL";
		    public static readonly string EDR = "ER";
		    public static readonly string PPG_INFRARED = "PI";
		    public static readonly string PPG_RED = "PR";
		    public static readonly string PPG_GREEN = "PG";
		    public static readonly string SPO2 = "O2";
		    public static readonly string TEMPERATURE_0 = "T0";
		    public static readonly string TEMPERATURE_1 = "T1";
		    public static readonly string THERMOPILE = "TH";
		    public static readonly string HUMIDITY_0 = "H0";
		    public static readonly string ACCELEROMETER_X = "AX";
		    public static readonly string ACCELEROMETER_Y = "AY";
		    public static readonly string ACCELEROMETER_Z = "AZ";
		    public static readonly string GYROSCOPE_X = "GX";
		    public static readonly string GYROSCOPE_Y = "GY";
		    public static readonly string GYROSCOPE_Z = "GZ";
		    public static readonly string MAGNETOMETER_X = "MX";
		    public static readonly string MAGNETOMETER_Y = "MY";
		    public static readonly string MAGNETOMETER_Z = "MZ";
		    public static readonly string BATTERY_VOLTAGE = "BV";
		    public static readonly string BATTERY_PERCENT = "B%";
		    public static readonly string BUTTON_PRESS_SHORT = "BS";
		    public static readonly string BUTTON_PRESS_LONG = "BL";
		    public static readonly string DATA_CLIPPING = "DC";
		    public static readonly string DATA_OVERFLOW = "DO";
		    public static readonly string SD_CARD_PERCENT = "SD";
		    public static readonly string RESET = "RS";
		    public static readonly string EMOTIBIT_DEBUG = "DB";
		    public static readonly string ACK = "AK";
		    public static readonly string NACK = "NK";
		    public static readonly string REQUEST_DATA = "RD";
		    public static readonly string TIMESTAMP_EMOTIBIT = "TE";
		    public static readonly string TIMESTAMP_LOCAL = "TL";
		    public static readonly string TIMESTAMP_UTC = "TU";
		    public static readonly string TIMESTAMP_CROSS_TIME = "TX";
		    public static readonly string EMOTIBIT_MODE = "EM";
		    public static readonly string EMOTIBIT_INFO = "EI";
		    public static readonly string HEART_RATE = "HR";
		    public static readonly string INTER_BEAT_INTERVAL = "BI";
		    public static readonly string SKIN_CONDUCTANCE_RESPONSE_AMPLITUDE = "SA";
		    public static readonly string SKIN_CONDUCTANCE_RESPONSE_FREQ = "SF";

		    public static readonly string SKIN_CONDUCTANCE_RESPONSE_RISE_TIME = "SR";

		    // Computer data TypeTags (sent over reliable channel e.g. Control)
		    public static readonly string GPS_LATLNG = "GL";
		    public static readonly string GPS_SPEED = "GS";
		    public static readonly string GPS_BEARING = "GB";
		    public static readonly string GPS_ALTITUDE = "GA";
		    public static readonly string USER_NOTE = "UN";

		    public static readonly string LSL_MARKER = "LM";

		    // Control TypeTags
		    public static readonly string RECORD_BEGIN = "RB";
		    public static readonly string RECORD_END = "RE";
		    public static readonly string MODE_NORMAL_POWER = "MN"; // Stops sending data timestamping should be accurate
		    public static readonly string MODE_LOW_POWER = "ML"; // Stops sending data timestamping should be accurate
		    public static readonly string MODE_MAX_LOW_POWER = "MM"; // Stops sending data timestamping accuracy drops
		    public static readonly string MODE_WIRELESS_OFF = "MO"; // Stops sending data timestamping should be accurate
		    public static readonly string MODE_HIBERNATE = "MH"; // Full shutdown of all operation
		    public static readonly string EMOTIBIT_DISCONNECT = "ED";
		    public static readonly string SERIAL_DATA_ON = "S+";

		    public static readonly string SERIAL_DATA_OFF = "S-";

		    // Advertising TypeTags
		    public static readonly string PING = "PN";
		    public static readonly string PONG = "PO";
		    public static readonly string HELLO_EMOTIBIT = "HE";
		    public static readonly string HELLO_HOST = "HH";

		    public static readonly string EMOTIBIT_CONNECT = "EC";

		    // WiFi Credential management TypeTags
		    public static readonly string WIFI_ADD = "WA";
		    public static readonly string WIFI_DELETE = "WD";

		    //Information Exchange TypeTags
		    public static readonly string LIST = "LS";
	    }

	    public class PayloadLabel
	    {
		    public static readonly string CONTROL_PORT = "CP";
		    public static readonly string DATA_PORT = "DP";
		    public static readonly string DEVICE_ID = "DI";
		    public static readonly string RECORDING_STATUS = "RS";
		    public static readonly string POWER_STATUS = "PS";
		    public static readonly string LSL_MARKER_RX_TIMESTAMP = "LR";
		    public static readonly string LSL_MARKER_SRC_TIMESTAMP = "LM";
		    public static readonly string LSL_LOCAL_CLOCK_TIMESTAMP = "LC";
		    public static readonly string LSL_MARKER_DATA = "LD";
	    }

	    public static char PACKET_DELIMITER_CSV = '\n';
	    public static int MALFORMED_HEADER = -1;
	    
	    public static ushort headerLength = 6;
	    public static ushort headerByteLength = 12;
	    public static ushort maxHeaderCharLength = 35; // 13+(1)+5+(1)+3+(1)+2+(1)+3+(1)+3+(1)

	    public static char PAYLOAD_DELIMITER = ',';
	    public static char PAYLOAD_TRUNCATED = (char) 25;	
	    
	   
		//const uint8_t nAperiodicTypeTags = 2;
		//const uint8_t nUserMessagesTypeTags = 1;
		// Defining TypeTag groups
		public class TypeTagGroups
		{
			public static readonly string[] APERIODIC = 
			{
				TypeTag.HEART_RATE, 
				TypeTag.INTER_BEAT_INTERVAL, 
				TypeTag.SKIN_CONDUCTANCE_RESPONSE_AMPLITUDE,
				TypeTag.SKIN_CONDUCTANCE_RESPONSE_RISE_TIME
			};

			public static int NUM_APERIODIC => APERIODIC.Length; // sizeof(APERIODIC[0]);
			 
			
			public static string[] USER_MESSAGES = {TypeTag.USER_NOTE};
			
			public static int NUM_USER_MESSAGES = USER_MESSAGES.Length;

			//vector<string> APERIODIC.push_back(DATA_CLIPPING);
			
			public static string[] COMPOSITE_PAYLOAD =
			{
				TypeTag.TIMESTAMP_CROSS_TIME,
				TypeTag.LSL_MARKER
			};
			
			public static int NUM_COMPOSITE_PAYLOAD = COMPOSITE_PAYLOAD.Length;

		}
    
		
		public static int maxTestLength = 200; // testing value

		#if ARDUINO
			public static string TIMESTAMP_STRING_FORMAT = "%Y-%m-%d_%H-%M-%S-%f";
		#else
			public const string TIMESTAMP_STRING_FORMAT = "yyyy-MM-dd_HH-mm-ss-ffffff";
		#endif


		#if ARDUINO
		//static String getTypeTag(const Header &h);
		#else
		//string EmotiBitPacket::getTypeTag(const Header h) {
		//}
		#endif

		//EmotiBitPacket::EmotiBitPacket() {
		//const string EmotiBitPacket::typeTags[(uint8_t)EmotiBitPacket::PacketType::length];
		//EmotiBitPacket::typeTags[(uint8_t)EmotiBitPacket::PacketType::EDA] = &EDA;
			//typeTags[PacketType::EDL] = EDL;
			//typeTags[PacketType::EDR] = EDR;
			//typeTags[PacketType::PPG_INFRARED] = PPG_INFRARED;
			//typeTags[PacketType::PPG_RED] = PPG_RED;
			//typeTags[PacketType::PPG_GREEN] = PPG_GREEN;
			//typeTags[PacketType::TEMPERATURE_0] = TEMPERATURE_0;
			//typeTags[PacketType::THERMISTOR] = THERMISTOR;
			//typeTags[PacketType::HUMIDITY_0] = HUMIDITY_0;
			//typeTags[PacketType::ACCELEROMETER_X] = ACCELEROMETER_X;
			//typeTags[PacketType::ACCELEROMETER_Y] = ACCELEROMETER_Y;
			//typeTags[PacketType::ACCELEROMETER_Z] = ACCELEROMETER_Z;
			//typeTags[PacketType::GYROSCOPE_X] = GYROSCOPE_X;
			//typeTags[PacketType::GYROSCOPE_Y] = GYROSCOPE_Y;
			//typeTags[PacketType::GYROSCOPE_Z] = GYROSCOPE_Z;
			//typeTags[PacketType::MAGNETOMETER_X] = MAGNETOMETER_X;
			//typeTags[PacketType::MAGNETOMETER_Y] = MAGNETOMETER_Y;
			//typeTags[PacketType::MAGNETOMETER_Z] = MAGNETOMETER_Z;
			//typeTags[PacketType::BATTERY_VOLTAGE] = BATTERY_VOLTAGE;
			//typeTags[PacketType::BATTERY_PERCENT] = BATTERY_PERCENT;
			//typeTags[PacketType::DATA_CLIPPING] = DATA_CLIPPING;
			//typeTags[PacketType::DATA_OVERFLOW] = DATA_OVERFLOW;
			//typeTags[PacketType::SD_CARD_PERCENT] = SD_CARD_PERCENT;
			//typeTags[PacketType::RESET] = RESET;
			//typeTags[PacketType::GPS_LATLNG] = GPS_LATLNG;
			//typeTags[PacketType::GPS_SPEED] = GPS_SPEED;
			//typeTags[PacketType::GPS_BEARING] = GPS_BEARING;
			//typeTags[PacketType::GPS_ALTITUDE] = GPS_ALTITUDE;
			//typeTags[PacketType::TIMESTAMP_LOCAL] = TIMESTAMP_LOCAL;
			//typeTags[PacketType::TIMESTAMP_UTC] = TIMESTAMP_UTC;
			//typeTags[PacketType::RECORD_BEGIN] = RECORD_BEGIN;
			//typeTags[PacketType::RECORD_END] = RECORD_END;
			//typeTags[PacketType::USER_NOTE] = USER_NOTE;
			//typeTags[PacketType::MODE_HIBERNATE] = MODE_HIBERNATE;
			//typeTags[PacketType::ACK] = ACK;
			//typeTags[PacketType::HELLO_EMOTIBIT] = HELLO_EMOTIBIT;
			//typeTags[PacketType::REQUEST_DATA] = REQUEST_DATA;
			//typeTags[PacketType::PING] = PING;
			//typeTags[PacketType::PONG] = PONG;
		//}


		public static int getHeader(string packet, out EmotibitHeader packetHeader)
		{
			int dataStartChar = 0;
			packetHeader = new  EmotibitHeader();
			int commaN;
			int commaN1;
			// timestamp
			commaN = 0;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER);
			if (commaN1 == -1 || commaN1 == 0) return MALFORMED_HEADER;
			packetHeader.Timestamp = uint.Parse(packet.Substring(commaN, commaN1-commaN));
			// packet_number
			commaN = commaN1 + 1;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, commaN);
			if (commaN1 == -1) return MALFORMED_HEADER;
			var substr = packet.Substring(commaN, commaN1-commaN).Trim();
			packetHeader.PacketNumber = ushort.Parse(substr);
			// data_length
			commaN = commaN1 + 1;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, commaN);
			if (commaN1 == -1) return MALFORMED_HEADER;
			packetHeader.DataLength = ushort.Parse(packet.Substring(commaN, commaN1-commaN).Trim());
			// typetag
			commaN = commaN1 + 1;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, commaN);
			if (commaN1 == -1) return MALFORMED_HEADER;
		#if ARDUINO
			// ToDo: Handle string = String more gracefully
			packetHeader.typeTag = packet.substring(commaN, commaN1);
		#else
			substr = packet.Substring(commaN, commaN1-commaN);
			packetHeader.TypeTag = new char[] { substr[0], substr[1] };
		#endif
			// protocol_version
			commaN = commaN1 + 1;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, commaN);
			if (commaN1 == -1) return MALFORMED_HEADER;
			packetHeader.ProtocolVersion = byte.Parse(packet.Substring(commaN, commaN1-commaN));
			// data_reliability
			commaN = commaN1 + 1;
			commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, commaN);
			if (commaN1 == -1) 
			{
				// handle case when no ,[data] exists
				commaN1 = packet.Length;
				dataStartChar = -1;
			}
			else
			{
				//dataStartChar = 11111;
				dataStartChar = commaN1 + 1;
			}
			packetHeader.DataReliability = byte.Parse(packet.Substring(commaN, commaN1-commaN));

			return dataStartChar;
		}
		public static bool getHeader(List<string> packet, ref EmotibitHeader packetHeader) 
		{

			if (packet.Count >= headerLength) {
				if (packet[0] != "") {
					packetHeader.Timestamp = uint.Parse(packet[0]);
				}
				else return false;

				if (packet[1] != "") {
					//ushort tempPacketNumber = stoi(packet.at(1));
					//if (tempPacketNumber - packetHeader.packetNumber > 1) {
					//	cout << "**  Missed packet: " << packetHeader.packetNumber << EmotiBitPacket::PAYLOAD_DELIMITER << tempPacketNumber << "**" << endl;
					//}
					// ToDo: Figure out a way to deal with multiple packets of each number
					//packetHeader.packetNumber = tempPacketNumber;
					packetHeader.PacketNumber = ushort.Parse(packet[1]);
				}
				else return false;

				if (packet[2] != "") {
					packetHeader.DataLength = ushort.Parse(packet[2]);
				}
				else return false;

				if (packet[3] != "")
				{
					packetHeader.TypeTag = new char[]
						{ packet[3][0], packet[3][1] };
				}
				else return false;

				if (packet[4] != "") {
					packetHeader.ProtocolVersion = byte.Parse(packet[4]);
				}
				else return false;

				if (packet[5] != "") {
					packetHeader.DataReliability = byte.Parse(packet[5]);
				}
				else return false;

				if (packet.Count < headerLength + packetHeader.DataLength) {
					//malformedMessages++;
					//cout << "**** MALFORMED MESSAGE " << malformedMessages << ", " << messageLen << " ****" << endl;
					return false;
				}

			}
			else {
				return false;
			}
			return true;
		}

		#if ARDUINO
		EmotiBitPacket::Header EmotiBitPacket::createHeader(const String &typeTag, uint32_t timestamp, ushort packetNumber, ushort dataLength, uint8_t protocolVersion, uint8_t dataReliability)
		#else
	    public static EmotibitHeader createHeader(string typeTag, uint timestamp, ushort packetNumber, ushort dataLength, byte protocolVersion, byte dataReliability)
		#endif
		{
			EmotibitHeader newHeader = new EmotibitHeader
			{
				TypeTag = new char[]
				{
					typeTag[0],
					typeTag[1],
				},
				Timestamp = timestamp,
				PacketNumber = packetNumber,
				DataLength = dataLength,
				ProtocolVersion = protocolVersion,
				DataReliability = dataReliability
			};

			return newHeader;
		}

		#if ARDUINO
		String EmotiBitPacket::headerToString(const Header &header)
		{
			String headerString;
			headerString = "";
			headerString += header.timestamp;
			headerString += EmotiBitPacket::PAYLOAD_DELIMITER;
			headerString += header.packetNumber;
			headerString += EmotiBitPacket::PAYLOAD_DELIMITER;
			headerString += header.dataLength;
			headerString += EmotiBitPacket::PAYLOAD_DELIMITER;
			headerString += header.typeTag;
			headerString += EmotiBitPacket::PAYLOAD_DELIMITER;
			headerString += header.protocolVersion;
			headerString += EmotiBitPacket::PAYLOAD_DELIMITER;
			headerString += header.dataReliability;
		#else
	    public static string headerToString(EmotibitHeader header)
		{
			//Refactored to use to_string instead of ofToString
			string headerString;
			headerString = "";
			headerString += header.Timestamp;
			headerString += PAYLOAD_DELIMITER;
			headerString += header.PacketNumber;
			headerString += PAYLOAD_DELIMITER;
			headerString += header.DataLength;
			headerString += PAYLOAD_DELIMITER;
			headerString += new string(header.TypeTag); //directly using string
			headerString += PAYLOAD_DELIMITER;
			headerString += header.ProtocolVersion;
			headerString += PAYLOAD_DELIMITER;
			headerString += header.DataReliability;
		#endif
			//createPacketHeader(tempHeader, timestamp, typeTag, dataLen);
			return headerString;
		}



	    public static int getPacketElement(string packet, out string element, int startChar)
		{
			int nextStartChar = -1;

			element = "";
			int commaN1 = packet.IndexOf(PAYLOAD_DELIMITER, startChar);

			if (commaN1 != -1) 
			{
				// A following comma was found, extract element
				element = packet.Substring(startChar, commaN1-startChar);
				if (packet.Length > commaN1 + 1)
				{
					nextStartChar = commaN1 + 1;
				}
			}
			else if (packet.Length > startChar + 1)
			{
				// No following comma was found, return final element of packet
				element = packet.Substring(startChar, packet.Length-startChar);
			}
			return nextStartChar;
		}

	    public static int getPacketKeyedValue(string packet, string key, out string value, int startChar)
		{
			string element;
			value = "";
			do
			{
				startChar = getPacketElement(packet, out element, startChar);
				if (element.Trim().CompareTo(key.Trim()) == 0)
				{
					getPacketElement(packet, out value, startChar);
					if (value.Length > 0) {
						return startChar;
					}
					return -1;	// return -1 if we hit the end of the packet before finding a value
				}
			} while (startChar > -1);

			return -1;	// return -1 if we hit the end of the packet before finding key
		}

		#if ARDUINO

		String EmotiBitPacket::createPacket(const String &typeTag, const ushort &packetNumber, const String &data, const ushort &dataLength, const uint8_t& protocolVersion, const uint8_t& dataReliability)
		{
			// ToDo: Generalize createPacket to work across more platforms inside EmotiBitPacket
			EmotiBitPacket::Header header = EmotiBitPacket::createHeader(typeTag, millis(), packetNumber, dataLength, protocolVersion, dataReliability);
			return EmotiBitPacket::headerToString(header) + EmotiBitPacket::PAYLOAD_DELIMITER + data + EmotiBitPacket::PACKET_DELIMITER_CSV;
		}
		#else

		//template <class T>
		//void EmotiBitPacket::addToPayload(const T &element, std::stringstream &payload, ushort &payloadLen)
		//{
		//	payload << element << EmotiBitPacket::PAYLOAD_DELIMITER;
		//	payloadLen++;
		//}
		//void EmotiBitPacket::addToPayload(const string element, std::stringstream &payload, ushort &payloadLen)

		public static short getPacketElement(string packet, out string element, ushort startChar)
		{
			element = "";
			// ToDo: try out a more passthrough approach to overloading
			short pos = getPacketElement(packet, out element, startChar);
			return pos;
		}
		
		public static short getPacketKeyedValue(string packet, out string key, out string value, ushort startChar)
		{
			// ToDo: try out a more passthrough approach to overloading
			short pos = getPacketKeyedValue(packet, out key, out value, startChar);
			return pos;
		}
		
		public static int getPacketKeyedValue(List<string> splitPacket, out string key, out string value, ushort startIndex)
		{
			key = "";
			value = "";
			for (int i = startIndex; i < splitPacket.Count; i++)
			{
				if (key.Equals(splitPacket[i]))
				{
					i++;
					value = splitPacket[i];
					return i;
				}
			}
			return -1;
		}

		public static string createPacket(string typeTag, ushort packetNumber, string data, ushort dataLength, byte protocolVersion = 1, byte dataReliability = 100)
		{

			EmotibitHeader header = createHeaderWithTime(typeTag, packetNumber, dataLength, protocolVersion, dataReliability);
			string packet = createPacket(header, data);
			return packet;
		}


		public static string createPacket(string typeTag, ushort packetNumber, List<string> data, byte protocolVersion = 1, byte dataReliability = 100)
		{
			// ToDo: Template data vector
			// ToDo: Generalize createPacket to work across more platforms inside EmotiBitPacket
			ushort dataLength = (ushort) data.Count;
			EmotibitHeader header = createHeaderWithTime(typeTag, packetNumber, dataLength, protocolVersion, dataReliability);

			string packet = headerToString(header);
			foreach (string s in data)
			{
				packet += PAYLOAD_DELIMITER + s;
			}
			packet += PACKET_DELIMITER_CSV;
			return packet;
		}
		
		public static string createPacket(EmotibitHeader header, string data)
		{
			var dataLength = header.DataLength;
			if (dataLength == 0)
				return headerToString(header) + PACKET_DELIMITER_CSV;
			else
			{
				String result = headerToString(header);
				result += PAYLOAD_DELIMITER;
				result += data;
				result += PACKET_DELIMITER_CSV;
				return result;
			}
		}

		public static EmotibitHeader createHeaderWithTime(string typeTag, ushort packetNumber, ushort dataLength, byte protocolVersion, byte dataReliability)
		{
			uint milliseconds = (uint) (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
			EmotibitHeader header = createHeader(typeTag, milliseconds, packetNumber, dataLength, protocolVersion, dataReliability);
			return header;
		}

		#endif
	    //ToDo: Edit function to be more modular in the future so we can add more test data messages
	    static bool firstMessage = true;
	    static int testCount = 0;
	    public static void createTestDataPacket(String dataMessage)
		{

		    dataMessage = "";

			// First message to signify start of test
		    if (firstMessage)
			{
		        firstMessage = false;
		        EmotibitHeader beginHeader = createHeader(TypeTag.USER_NOTE, 0, 0, 1, 0, 0);
				String data = "";
				dataMessage = createPacket(beginHeader, data);
			}

		    else if (testCount <= maxTestLength)
			{
		        int dataLength = 0;
				
				String data = createTestSawtoothData(out dataLength); //Set data first so dataLength is set for the header
				EmotibitHeader header = createTestHeader((ushort)dataLength); 

				dataMessage = createPacket(header, data);
		        testCount++;
		    }

			// End case to visually signal end of test
			else if (testCount == maxTestLength + 1)
			{
				 EmotibitHeader endHeader = createHeader(TypeTag.EDA, 0, 0, 1, 0, 0);
				String data = "";
				dataMessage = createPacket(endHeader, data);
				testCount++;
			}

		}

	    public static String createTestSawtoothData(out int outLength)
		{
		    String payload = "";

		    int numValues = 10; // Number of values to generate
		    int minVal = 0; // Minimum value
		    int maxVal = 100; // Maximum value
			for (short i = 0; i < numValues; ++i)
			{
		        if (i > 0) payload += PAYLOAD_DELIMITER;
		        int value = minVal + ((maxVal - minVal) * i) / (numValues - 1);
		        payload += value;
		    }
			outLength = numValues;
		    return payload;
		}
		
		static uint timestamp = 0;
		static ushort packetNumber = 0;
		static byte protocolVersion = 0;
		static byte dataReliability = 0;
		public static EmotibitHeader createTestHeader(ushort dataLength)
		{
			

		    EmotibitHeader header = createHeader(
		        TypeTag.EDA,
				timestamp++,
		        packetNumber++,
		        dataLength,
		        protocolVersion++,
		        dataReliability++
		    );
		    return header;
		}
    }
}