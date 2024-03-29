﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin;
using Lotlab.PluginCommon.Overlay;

namespace GardeningTracker
{
    public partial class PluginGardeningTracker : IOverlayAddonV2
    {
        void IOverlayAddonV2.Init()
        {
            var container = Registry.GetContainer();
            var registry = container.Resolve<Registry>();

            // Register events
            var eventSource = new GardeningTrackerEventSource(container, tracker);
            tracker.OnSyncContent += eventSource.ChangeGardeningData;
            tracker.Storage.OnDataChange += () =>
            {
                eventSource.ChangeGardeningData(tracker.Storage.GetStorageItems());
            };
            tracker.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(tracker.CurrentZone):
                        eventSource.ChangeGardeningZone(tracker.CurrentZone);
                        break;
                    default:
                        break;
                }
            };

            // Register EventSource
            // registry.StartEventSource(eventSource);
            // API 兼容: commit 0ff9f5e change API
            var method = registry.GetType().GetMethod(nameof(registry.StartEventSource));
            if (method.IsGenericMethod)
                method = method.MakeGenericMethod(typeof(GardeningTrackerEventSource));
            
            method.Invoke(registry, new object[] { eventSource });

            // Register Overlay
            registry.RegisterOverlayPreset2(new GardeningTrackerOverlayPresent());

            tracker.OverlayInited = true;
        }
    }

    class GardeningTrackerEventSource : EventSourceBase
    {
        const string EventGardeningDataChange = "onGardeningDataChange";
        const string EventGardeningZoneChange = "onGardeningZoneChange";
        const string OnRequestGardeningData = "RequestGardeningData";

        GardeningTracker tracker { get; }

        public GardeningTrackerEventSource(TinyIoCContainer c, GardeningTracker t) : base(c)
        {
            tracker = t;

            Name = "园艺时钟";

            RegisterEventTypes(new List<string>()
            {
                EventGardeningDataChange,
                EventGardeningZoneChange
            });

            RegisterEventHandler(OnRequestGardeningData, (obj) =>
            {
                return JObject.FromObject(new
                {
                    garden = tracker.Storage.GetStorageItems()
                });
            });
        }

        public void ChangeGardeningData(IEnumerable<GardeningItem> items)
        {
            DispatchEvent(JObject.FromObject(new
            {
                type = EventGardeningDataChange,
                data = items
            }));
        }

        public void ChangeGardeningZone(CurrentZone zone)
        {
            DispatchEvent(JObject.FromObject(new
            {
                type = EventGardeningZoneChange,
                data = zone
            }));
        }

        public override Control CreateConfigControl()
        {
            return null;
        }

        public override void LoadConfig(IPluginConfig config)
        {
            // do nothing
        }

        public override void SaveConfig(IPluginConfig config)
        {
            // do nothing
        }
    }

    class GardeningTrackerOverlayPresent : IOverlayPreset
    {
        string IOverlayPreset.Name => "园艺时钟";

        string IOverlayPreset.Type => "MiniParse";

        string IOverlayPreset.Url => "https://xivclock.lotlab.org/";

        int[] IOverlayPreset.Size => new int[2] { 300, 500 };

        bool IOverlayPreset.Locked => false;

        List<string> IOverlayPreset.Supports => new List<string>() { "modern" };

        public override string ToString()
        {
            return ((IOverlayPreset)this).Name;
        }
    }

}
