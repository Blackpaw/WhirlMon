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

        public class WatchedForumsGroup : ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED>
        {
            public WatchedForumsGroup(IEnumerable<WhirlMon.WhirlPoolAPIData.WATCHED> items) : base(items)
            {
            }

            public string Forums { get; set; }
        }

        public class NewsDateGroup : ObservableCollection<WhirlMon.WhirlPoolAPIData.NEWS>
        {
            public NewsDateGroup(IEnumerable<WhirlMon.WhirlPoolAPIData.NEWS> items) : base(items)
            {
            }

            public DateTime Date { get; set; }
            public string DOW { get { return Date.DayOfWeek.ToString(); } }
            public string SHORTDATE { get { return WhirlMon.PrettyDate.ToShortDate(Date); } }
        }

        static public void UpdateUIData(WhirlMon.WhirlPoolAPIData.RootObject root)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                var r = (WhirlMon.WhirlPoolAPIData.RootObject) o;

                // Watched
                IEnumerable<WatchedForumsGroup> watched =
                    from item in r.WATCHED
                    group item by item.FORUM_NAME into forumGroup
                    select new WatchedForumsGroup(forumGroup)
                    {
                        Forums = forumGroup.Key
                    };                
                var cvsWatched = (CollectionViewSource)Application.Current.Resources["srcWatched"];
                cvsWatched.Source = watched.ToList();

                // news
                IEnumerable<NewsDateGroup> news =
                    from item in r.NEWS
                    group item by item.DATE_D.Date into dateGroup
                    select new NewsDateGroup(dateGroup)
                    {
                        Date = dateGroup.Key
                    };
                var cvsNews = (CollectionViewSource)Application.Current.Resources["srcNews"];
                cvsNews.Source = news.ToList();

            }), root);
        }
    }
}
