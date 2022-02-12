using Lotlab.PluginCommon.FFXIV.Parser.Packets;
using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GardeningTracker.Packets
{
    /// <summary>
    /// Might be change zone
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FFXIVIpcGuessZoneInto
    {
        public IPCHeader ipc;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public LandIdent[] idents;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] serverName;
    }

    class GuessZoneInto : IPCPacketBase<FFXIVIpcGuessZoneInto>
    {
        public LandIdent Area => Value.idents[1];
        public LandIdent House => Value.idents[2];
        public string ServerName => Value.serverName.GetUTF8String();

        public override string ToString()
        {
            return $"(?) Change Zone. Server: {ServerName}, 0: {Value.idents[0]}, Area: {Value.idents[1]}, House: {Value.idents[2]}, 3: {Value.idents[3]}";
        }
    }
}
