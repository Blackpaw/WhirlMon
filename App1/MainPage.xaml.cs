using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WhirlMonApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        static public ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED> watchedItems = new ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED>();

        static private SynchronizationContext synchronizationContext;

        public MainPage()
        {
            this.InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            lvWatched.DataContext = watchedItems;
            lvRecent.DataContext = watchedItems;

            WhirlMon.WhirlPoolAPIClient.GetWatchedAsync(true);
        }

        static public void UpdateUIData(WhirlMon.WhirlPoolAPIData.RootObject root)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                var r = (WhirlMon.WhirlPoolAPIData.RootObject) o;

                watchedItems.Clear();
                foreach (var item in r.WATCHED)
                {
                    watchedItems.Add(item);
                }
            }), root);
        }
    }
}
