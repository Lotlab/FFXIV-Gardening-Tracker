using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be action to target
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessTargetAction
    {
        public IPCHeader ipc;

        public UInt32 targetID;
        public UInt32 flags;
        public UInt32 operation;
        public UInt32 padding;
    }

    class GuessTargetAction : IPCPacketBase<FFXIVIpcGuessTargetAction>
    {
        public override string ToString()
        {
            return $"(?) Target Operation. TargetID: {Value.targetID}, Operation: {Value.operation}, Flags: {Value.flags}";
        }
    }
}
