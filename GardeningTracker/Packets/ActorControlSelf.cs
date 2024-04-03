using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// ActorControlSelf
    /// </summary>
    class ActorControlSelf : IPCPacketBase<FFXIVIpcActorControlSelf>
    {
        public FFXIVIpcActorControlType Category => (FFXIVIpcActorControlType)Value.category;

        public UInt32[] Param => Value.param;

        public override string ToString()
        {
            return $"Actor control self. Category: {Value.category}, Params: {String.Join(", ", Value.param.Select(x => x.ToString()))}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcActorControlSelf
    {
        public IPCHeader ipc;
        public UInt16 category;
        public UInt16 padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public UInt32[] param;
    }

    enum FFXIVIpcActorControlType
    {
        UpdateRestedExp = 0x18,
        // strip unused
        UpdateGardeningState = 0x3fc,
        SetHarvestResult = 0x3fd,
        UpdateGardeningState2 = 0x3fe,
    }

    struct HarvestResult
    {
        public uint Result1ID;
        public uint Result1Count;
        public uint Result1Seed;
        public uint Result2ID;
        public uint Result2Count;
        public uint Result2Seed;

        public HarvestResult(uint[] data)
        {
            Result1ID = data[0];
            Result1Count = data[1];
            Result1Seed = data[2];
            Result2ID = data[3];
            Result2Count = data[4];
            Result2Seed = data[5];
        }
    }
}
