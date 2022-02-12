using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Runtime.InteropServices;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be operation target binding confirm
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessTargetConfirm
    {
        public IPCHeader ipc;

        public UInt32 actorID;
        public UInt32 unknown1;
        public UInt32 targetID;
        public UInt32 unknown2;
        public UInt32 unknown3;
        public UInt32 unknown4;
    }

    /// <summary>
    /// 
    /// </summary>
    class GuessTargetConfirm : IPCPacketBase<FFXIVIpcGuessTargetConfirm>
    {
        public override string ToString()
        {
            return $"(?) Target Confirm. ActorID: {Value.actorID}, TargetID: {Value.targetID}, Unk1: {Value.unknown1}, Unk2: {Value.unknown2}, Unk3: {Value.unknown3}, Unk4: {Value.unknown4}";
        }
    }
}
