using System;
using System.Runtime.InteropServices;

namespace SenseXR.Core.Demo.OSC.Emotibit.Utils
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct EmotibitHeader
    {
        public UInt32 Timestamp;

        public UInt16 PacketNumber;

        public UInt16 DataLength;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
        public char[] TypeTag;

        public byte ProtocolVersion;

        public byte DataReliability;
    }
}