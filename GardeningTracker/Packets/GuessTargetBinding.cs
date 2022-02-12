using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Runtime.InteropServices;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be operation target binding
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessTargetBinding
    {
        public IPCHeader ipc;

        public UInt32 actorID;
        public UInt32 unknown1;
        public UInt32 targetID;
        public UInt32 unknown2;
    }

    class GuessTargetBinding : IPCPacketBase<FFXIVIpcGuessTargetBinding>
    {
        public override string ToString()
        {
            return $"(?) Target Bind. ActorID: {Value.actorID}, TargetID: {Value.targetID}, Unk1: {Value.unknown1}";
        }
    }
}
