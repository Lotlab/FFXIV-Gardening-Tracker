using Lotlab.PluginCommon.FFXIV.Parser;
using System;
using System.Runtime.InteropServices;

namespace GardeningTracker.Packets
{
    enum InventoryOperation
    {
        Discard = 320,
        Move,
        Swap,
        Split,
        Merge,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcInventoryModifyHandler
    {
        public IPCHeader ipc;

        public UInt32 sequence;
        public UInt16 action; //  Move: 0x0223, Swap: 0x0224, Split=0x0225, Merge=0x0227
        public UInt16 unknown1;
        public UInt32 padding;
        public UInt32 fromContainer;
        public UInt16 fromSlot;
        public UInt16 unknown2;
        public UInt32 fromQuantity;
        public UInt32 unknown3;
        public UInt32 unknown4;
        public UInt32 toContainer;
        public UInt16 toSlot;
        public UInt16 unknown5;
        public UInt32 toQuantity;
        public UInt32 unknown6;
    }

    class InventoryModify : IPCPacketBase<FFXIVIpcInventoryModifyHandler>
    {
        public override string ToString()
        {
            return $"InventoryModify. Action: {(InventoryOperation)Value.action}, From: ({Value.fromContainer}, {Value.fromSlot})[{Value.fromQuantity}], To: ({Value.toContainer}, {Value.toSlot})[{Value.toQuantity}]";
        }
    }
}
