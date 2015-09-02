using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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

            public int forumId { get; set; }
            public string Forum { get; set; }
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
                        Forum = forumGroup.Key,
                        forumId = forumGroup.ElementAtOrDefault(0).FORUM_ID
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

        private async void lvWatched_Tapped(object sender, TappedRoutedEventArgs e)
        {
            WhirlMon.WhirlPoolAPIData.WATCHED w = (WhirlMon.WhirlPoolAPIData.WATCHED)lvWatched.SelectedItem;

            string url =
                   string.Format(@"http://forums.whirlpool.net.au/forum-replies.cfm?t={0}&p={1}&#r{2}", w.ID, w.LASTPAGE, w.LASTREAD);
            var uri = new Uri(url);
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void lvWatchedForum_Tapped(object sender, TappedRoutedEventArgs e)
        {            
            FrameworkElement forum = e.OriginalSource as FrameworkElement;
            var uri = new Uri(String.Format(@"https://forums.whirlpool.net.au/forum/{0}", forum.Tag));
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void News_Tapped(object sender, TappedRoutedEventArgs e)
        {
            WhirlMon.WhirlPoolAPIData.NEWS news = (WhirlMon.WhirlPoolAPIData.NEWS) lvNews.SelectedItem;

            var uri = new Uri(String.Format(@"http://whirlpool.net.au/news/go.cfm?article={0}", news.ID));
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WhirlMon.WhirlPoolAPIClient.GetWatchedAsync(true);
        }
    }
}
