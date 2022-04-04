using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
namespace GardeningTracker
{
    /// <summary>
    /// PluginControlWpf.xaml 的交互逻辑
    /// </summary>
    public partial class PluginControl : UserControl
    {
        public PluginControl()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    public class AutoScrollListBox : ListBox
    {
        ///<summary>
        ///Define the AutoScroll property. If enabled, causes the ListBox to scroll to 
        ///the last item whenever a new item is added.
        ///</summary>
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(
                "AutoScroll",
                typeof(Boolean),
                typeof(AutoScrollListBox),
                new FrameworkPropertyMetadata(
                    true, //Default value.
                    FrameworkPropertyMetadataOptions.AffectsArrange |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    AutoScroll_PropertyChanged));

        /// <summary>
        /// Gets or sets whether or not the list should scroll to the last item 
        /// when a new item is added.
        /// </summary>
        [Category("Common")] //Indicate where the property is located in VS designer.
        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }

        /// <summary>
        /// Event handler for when the AutoScroll property is changed.
        /// This delegates the call to SubscribeToAutoScroll_ItemsCollectionChanged().
        /// </summary>
        /// <param name="d">The DependencyObject whose property was changed.</param>
        /// <param name="e">Change event args.</param>
        private static void AutoScroll_PropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SubscribeToAutoScroll_ItemsCollectionChanged(
                (AutoScrollListBox)d,
                (bool)e.NewValue);
        }

        /// <summary>
        /// Subscribes to the list items' collection changed event if AutoScroll is enabled.
        /// Otherwise, it unsubscribes from that event.
        /// For this to work, the underlying list must implement INotifyCollectionChanged.
        ///
        /// (This function was only creative for brevity)
        /// </summary>
        /// <param name="listBox">The list box containing the items collection.</param>
        /// <param name="subscribe">Subscribe to the collection changed event?</param>
        private static void SubscribeToAutoScroll_ItemsCollectionChanged(
            AutoScrollListBox listBox, bool subscribe)
        {
            INotifyCollectionChanged notifyCollection =
                listBox.Items.SourceCollection as INotifyCollectionChanged;
            if (notifyCollection != null)
            {
                if (subscribe)
                {
                    //AutoScroll is turned on, subscribe to collection changed events.
                    notifyCollection.CollectionChanged +=
                        listBox.AutoScroll_ItemsCollectionChanged;
                }
                else
                {
                    //AutoScroll is turned off, unsubscribe from collection changed events.
                    notifyCollection.CollectionChanged -=
                        listBox.AutoScroll_ItemsCollectionChanged;
                }
            }
        }

        /// <summary>
        /// Event handler called only when the ItemCollection changes
        /// and if AutoScroll is enabled.
        /// </summary>
        /// <param name="sender">The ItemCollection.</param>
        /// <param name="e">Change event args.</param>
        private void AutoScroll_ItemsCollectionChanged(
            object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                int count = Items.Count;
                ScrollIntoView(Items[count - 1]);
            }
        }

        /// <summary>
        /// Constructor a new LoggingListBox.
        /// </summary>
        public AutoScrollListBox()
        {
            //Subscribe to the AutoScroll property's items collection 
            //changed handler by default if AutoScroll is enabled by default.
            SubscribeToAutoScroll_ItemsCollectionChanged(
                this, (bool)AutoScrollProperty.DefaultMetadata.DefaultValue);
        }
    }

    [ValueConversion(typeof(Int64), typeof(string))]
    public class UnixTimestampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Int64 timestamp = System.Convert.ToInt64(value);
                if (timestamp == 0) return "-";

                return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "-";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string str = System.Convert.ToString(value);
                var result = DateTimeOffset.ParseExact(str, "yyyy-MM-dd HH:mm:ss", new CultureInfo("zh-CN"));
                return result.ToUnixTimeSeconds();
            }
            catch
            {
                return 0;
            }
        }
    }
}
