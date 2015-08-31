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
        public class WatchedList : ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED>{}


        static private SynchronizationContext synchronizationContext;

        public MainPage()
        {
            this.InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            WhirlMon.WhirlPoolAPIClient.GetWatchedAsync(true);
        }

        public class ForumsGroup : ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED>
        {
            public ForumsGroup(IEnumerable<WhirlMon.WhirlPoolAPIData.WATCHED> items) : base(items)
            {
            }

            public string Forums { get; set; }
        }

        static public void UpdateUIData(WhirlMon.WhirlPoolAPIData.RootObject root)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                var r = (WhirlMon.WhirlPoolAPIData.RootObject) o;

                IEnumerable<ForumsGroup> groups =
                    from item in r.WATCHED
                    group item by item.FORUM_NAME into forumGroup
                    select new ForumsGroup(forumGroup)
                    {
                        Forums = forumGroup.Key
                    };

                
                var cvsWatched = (CollectionViewSource)Application.Current.Resources["srcWatched"];
                cvsWatched.Source = groups.ToList();
            }), root);
        }
    }
}
