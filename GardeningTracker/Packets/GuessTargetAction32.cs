using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be action to target, 32bytes param
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessTargetAction32
    {
        public IPCHeader ipc;

        public UInt32 targetID;
        public UInt32 unknown1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] param;
    }

    class GuessTargetAction32 : IPCPacketBase<FFXIVIpcGuessTargetAction32>
    {
        public override string ToString()
        {
            return $"(?) Action32. TargetID: {Value.targetID}, Unk1: {Value.unknown1}, Param: {Value.param.ToHexString()}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TargetAction32Sowing
    {
        public UInt32 unknown1;
        public FFXIVItemLong soil;
        public FFXIVItemLong seed;

        public UInt32 padding1;
        public UInt32 padding2;
        public UInt32 padding3;

        public override string ToString()
        {
            return $"(?) Sowing. SoilAt: {soil}, SeedAt: {seed}";
        }
    }
}
