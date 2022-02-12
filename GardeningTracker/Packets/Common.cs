using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVItemShort
    {
        public UInt16 containerID;
        public UInt16 slotID;

        public override string ToString()
        {
            return $"({containerID}, {slotID})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVItemLong
    {
        public UInt32 containerID;
        public UInt32 slotID;

        public override string ToString()
        {
            return $"({containerID}, {slotID})";
        }

        public FFXIVItemShort GetShort()
        {
            return new FFXIVItemShort() { containerID = (UInt16)containerID, slotID = (UInt16)slotID };
        }
    }
}
