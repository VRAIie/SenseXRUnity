using System;
using System.Collections.Generic;

namespace SenseXR.Core.Demo.OSC.Emotibit.Utils
{
    [Serializable]
    public class EmotibitConfig
    {
        public int SendAdvertisingInterval = 1000; // msec interval between advertising blasts

        public int CheckAdvertisingInterval = 100; // msec interval between checks for advertising replies

        public int AdvertisingThreadSleep = 1000;	// usec duration to sleep between thread loops

        public int DataThreadSleep = 1000;	// usec duration to sleep between thread loops

        public bool EnableBroadcast = true;

        public bool EnableUnicast = true;

        public bool DebugLogs = false;

        public int UnicastIpRangeLowerBound = 2; 

        public int UnicastIpRangeUpperBound = 254;

        public int NUnicastIpsPerLoop = 1;

        public int UnicastMinLoopDelay = 3;
        
        public List<string> LocalIPs = new() { "192.168.1.188","192.168.1.167" };

        public List<string> NetworkIncludeList = new() { "*.*.*.*" };

        public List<string> NetworkExcludeList =new() { "0.0.0.0" };
    }
}