using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be action to target, 16bytes param
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessTargetAction16
    {
        public IPCHeader ipc;

        public UInt32 targetID;
        public UInt32 unknown1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] param;
    }

    class GuessTargetAction16 : IPCPacketBase<FFXIVIpcGuessTargetAction16>
    {
        public override string ToString()
        {
            return $"(?) Action16. TargetID: {Value.targetID}, Unk1: {Value.unknown1}, Param: {Value.param.ToHexString()}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TargetAction16Fertilize
    {
        public UInt32 unknown1;
        public FFXIVItemLong fertilizer;
        public UInt32 padding;
    }
}
