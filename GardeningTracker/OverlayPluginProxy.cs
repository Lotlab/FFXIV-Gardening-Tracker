using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin;

namespace GardeningTracker
{
    public class OverlayPluginProxy
    {
        public static Type GetOverlayPluginType(string typeName)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in asm)
            {
                Type t = assembly.GetType(typeName, false);
                if (t != null)
                    return t;
            }
            return null;
        }
    }

    abstract class SimpleOverlayEventSourceBase : IEventSource
    {
        public string Name { get; protected set; }
        protected TinyIoCContainer container { get; }
        protected object dispatcher { get; }
        protected Type dispatcherType { get; }

        protected ILogger logger { get; }

        public SimpleOverlayEventSourceBase(TinyIoCContainer c)
        {
            container = c;
            logger = container.Resolve<ILogger>();

            dispatcherType = OverlayPluginProxy.GetOverlayPluginType("RainbowMage.OverlayPlugin.EventDispatcher");
            dispatcher = container.Resolve(dispatcherType);
        }

        public abstract Control CreateConfigControl();
        public abstract void LoadConfig(IPluginConfig config);
        public abstract void SaveConfig(IPluginConfig config);

        public virtual void Dispose() { }

        public virtual void Start() { }

        public virtual void Stop() { }

        protected void RegisterEventTypes(List<string> types)
        {
            dispatcherType.GetMethod("RegisterEventTypes", new Type[] { typeof(List<string>) })
                .Invoke(dispatcher, new object[] { types });
        }
        protected void RegisterEventType(string type)
        {
            dispatcherType.GetMethod("RegisterEventType", new Type[] { typeof(string) })
                .Invoke(dispatcher, new object[] { type });
        }
        protected void RegisterEventType(string type, Func<JObject> initCallback)
        {
            dispatcherType.GetMethod("RegisterEventType", new Type[] { typeof(string), typeof(Func<JObject>) })
                .Invoke(dispatcher, new object[] { type, initCallback });
        }

        protected void RegisterEventHandler(string name, Func<JObject, JToken> handler)
        {
            dispatcherType.GetMethod("RegisterHandler", new Type[] { typeof(string), typeof(Func<JObject, JToken>) })
                .Invoke(dispatcher, new object[] { name, handler });
        }

        protected void DispatchEvent(JObject e)
        {
            dispatcherType.GetMethod("RegisterHandler", new Type[] { typeof(JObject) })
                .Invoke(dispatcher, new object[] { e });
        }

        protected bool HasSubscriber(string eventName)
        {
            return (bool)dispatcherType.GetMethod("HasSubscriber").Invoke(dispatcherType, new object[] { eventName });
        }
    }
}

