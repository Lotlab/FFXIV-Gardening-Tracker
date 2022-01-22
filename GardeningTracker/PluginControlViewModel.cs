using Lotlab;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace GardeningTracker
{
    public class PluginControlViewModel : PropertyNotifier
    {
        public ObservableCollection<LogItem> Logs => logger.ObserveLogs;

        SimpleLogger logger;

        public PluginControlViewModel(Config config, SimpleLogger logger)
        {
            this.logger = logger;
        }

        string currentZone;
        public string CurrentZone
        {
            get => currentZone; 
            set
            {
                currentZone = value;
                OnPropertyChanged();
            }
        }
    }
}

