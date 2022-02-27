using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcObjectExternalData
    {
        public IPCHeader ipc;

        public UInt32 housingLink;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
        public byte[] data;
    }

    class ObjectExteralData : IPCPacketBase<FFXIVIpcObjectExternalData>
    {
        public override string ToString()
        {
            return $"Object External Data. HousingLink: {Value.housingLink}, Data: {Value.data.ToHexString()}";
        }
    }


    /// <summary>
    /// Land external data layout for FFXIVIpcObjectExternalData
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ObjectExternalDataLand
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt16[] Seed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] State;
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Land Data: ");
            for (int i = 0; i < 8; i++)
            {
                sb.Append("(");
                sb.Append(Seed[i].ToString());
                sb.Append(",");
                sb.Append(State[i].ToString());
                sb.Append(")");
                if (i != 7) sb.Append(", ");
            }
            return sb.ToString();
        }
    }

}
