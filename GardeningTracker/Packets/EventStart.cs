using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Runtime.InteropServices;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// packet sent by the server to start an event, not actually playing it, but registering
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcEventStart
    {
        public IPCHeader ipc;

        public UInt32 actorID;
        public UInt32 unknown1;
        public UInt32 targetID;
        public byte param1;
        public byte param2;
        public UInt16 padding;
        public UInt32 param3;
        public UInt32 padding1;
    }

    /// <summary>
    /// 
    /// </summary>
    class EventStart : IPCPacketBase<FFXIVIpcEventStart>
    {
        public override string ToString()
        {
            return $"Event start. ActorID: {Value.actorID}, TargetID: {Value.targetID}, Param1: {Value.param1}, Param2: {Value.param2}, Param3: {Value.param3}";
        }
    }
}
