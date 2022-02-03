using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;

namespace GardeningTracker
{
    public delegate void NetworkReceivedDelegate(string connection, long epoch, byte[] message);
    public delegate void NetworkSentDelegate(string connection, long epoch, byte[] message);
    public delegate void LogLineDelegate(uint EventType, uint Seconds, string logline);

    public class FFXIVPluginProxy
    {
        public bool Inited { get; private set; }

        object _ffxivDataRepo = null;
        object _ffxivDataSub = null;

        public event NetworkReceivedDelegate NetworkReceived;
        public event NetworkSentDelegate NetworkSent;
        public event LogLineDelegate LogLine;

        Delegate networkRecvDelegate;
        Delegate networkSentDelegate;
        Delegate logLineDelegate;

        public void InitPlugin()
        {
            if (Inited)
                return;

            var plugins = ActGlobals.oFormActMain.ActPlugins;
            foreach (var item in plugins)
            {
                if (item.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_PLUGIN"))
                {
                    _ffxivDataSub = item.pluginObj.GetType().GetProperty("DataSubscription").GetValue(item.pluginObj);
                    _ffxivDataRepo = item.pluginObj.GetType().GetProperty("DataRepository").GetValue(item.pluginObj);

                    if (_ffxivDataSub != null)
                    {
                        networkRecvDelegate = eventAdd(_ffxivDataSub, "NetworkReceived", (Action<string, long, byte[]>)networkReceivedProxy);
                        networkSentDelegate = eventAdd(_ffxivDataSub, "NetworkSent", (Action<string, long, byte[]>)networkSentProxy);
                        logLineDelegate = eventAdd(_ffxivDataSub, "LogLine", (Action<uint, uint, string>)logLineProxy);
                        Inited = true;
                    }
                }
            }
        }

        public void DeinitPlugin()
        {
            eventRemove(_ffxivDataSub, "NetworkReceived", networkRecvDelegate);
            eventRemove(_ffxivDataSub, "NetworkSent", networkSentDelegate);
            eventRemove(_ffxivDataSub, "LogLine", logLineDelegate);
            _ffxivDataSub = null;
            Inited = false;
        }

        static Delegate ConvertDelegate(Delegate sourceDelegate, Type targetType)
        {
            return Delegate.CreateDelegate(
                    targetType,
                    sourceDelegate.Target,
                    sourceDelegate.Method);
        }

        Delegate eventAdd(object obj, string eventName, Delegate handler)
        {
            var del = ConvertDelegate(handler, obj.GetType().GetEvent(eventName).EventHandlerType);
            obj.GetType().GetEvent(eventName).AddEventHandler(obj, del);
            return del;
        }

        void eventRemove(object obj, string eventName, Delegate handler)
        {
            obj.GetType().GetEvent(eventName).RemoveEventHandler(obj, handler);
        }

        void networkReceivedProxy(string connection, long epoch, byte[] message)
        {
            NetworkReceived?.Invoke(connection, epoch, message);
        }

        void networkSentProxy(string connection, long epoch, byte[] message)
        {
            NetworkSent?.Invoke(connection, epoch, message);
        }

        void logLineProxy(uint EventType, uint Seconds, string logline)
        {
            LogLine?.Invoke(EventType, Seconds, logline);
        }

        public uint GetWorldID()
        {
            var list = (IReadOnlyCollection<object>)_ffxivDataRepo.GetType().GetMethod("GetCombatantList").Invoke(_ffxivDataRepo, null);
            var currentID = (uint)_ffxivDataRepo.GetType().GetMethod("GetCurrentPlayerID").Invoke(_ffxivDataRepo, null);
            foreach (var item in list)
            {
                uint id = (uint)item.GetType().GetProperty("ID").GetValue(item);
                uint worldID = (uint)item.GetType().GetProperty("CurrentWorldID").GetValue(item);

                if (id == currentID)
                    return worldID;
            }

            return 0;
        }
    }
}

