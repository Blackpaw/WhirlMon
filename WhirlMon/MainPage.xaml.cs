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

        public class WatchedThreads : ObservableCollection<WhirlMon.WhirlPoolAPIData.WATCHED>
        {
            public WatchedThreads(IEnumerable<WhirlMon.WhirlPoolAPIData.WATCHED> items) : base(items)
            {
            }

            public int forumId { get; set; }
            public string Forum { get; set; }
        }

        public class ThreadForumGroups : ObservableCollection<WatchedThreads>
        {
            public ThreadForumGroups(IEnumerable<WatchedThreads> items) : base(items) { }
        }

        public class NewsItems : ObservableCollection<WhirlMon.WhirlPoolAPIData.NEWS>
        {
            public NewsItems(IEnumerable<WhirlMon.WhirlPoolAPIData.NEWS> items) : base(items)
            {
            }

            public DateTime Date { get; set; }
            public string DOW { get { return Date.DayOfWeek.ToString(); } }
            public string SHORTDATE { get { return WhirlMon.PrettyDate.ToShortDate(Date); } }
        }

        public class NewsDateGroup : ObservableCollection<NewsItems>
        {
            public NewsDateGroup(IEnumerable<NewsItems> items) : base(items) { }
        }

        static public void UpdateUIData(WhirlMon.WhirlPoolAPIData.RootObject root)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                var r = (WhirlMon.WhirlPoolAPIData.RootObject) o;

                // Watched
                if (r.WATCHED != null)
                {
                    // new
                    IEnumerable<WatchedThreads> watched =
                        from item in r.WATCHED
                        group item by item.FORUM_NAME into threadGroup
                        select new WatchedThreads(threadGroup)
                        {
                            Forum = threadGroup.Key,
                            forumId = threadGroup.ElementAtOrDefault(0).FORUM_ID
                        };

                    var grpWatched = new ThreadForumGroups(watched);
                    var cvsWatched = (CollectionViewSource)Application.Current.Resources["srcWatched"];
                    if (cvsWatched.Source == null)
                        cvsWatched.Source = grpWatched;
                    else
                    {
                        // Merge
                        var current = (ThreadForumGroups)cvsWatched.Source;

                        // Update all thread groups and threads
                        for (int gIdx = current.Count - 1; gIdx >= 0; gIdx--)
                        {
                            WatchedThreads grp = current[gIdx];
                            var _grp = grpWatched.SingleOrDefault(g => g.forumId == grp.forumId);
                            if (_grp == null)
                            {
                                // group no longer exists
                                current.RemoveAt(gIdx);
                                continue;
                            }

                            // Check threads in group
                            for(var tIdx = grp.Count - 1; tIdx >= 0; tIdx--)
                            {
                                WhirlMon.WhirlPoolAPIData.WATCHED wItem = grp[tIdx];
                                var _w = _grp.SingleOrDefault(w => w.ID == wItem.ID);
                                if (_w == null)
                                {
                                    // Remove thread
                                    grp.RemoveAt(tIdx);
                                    continue;
                                }

                                // Update?
                                if (!wItem.Equals(_w))
                                {
                                    wItem.LAST = _w.LAST;
                                    wItem.LAST_DATE = _w.LAST_DATE;
                                    wItem.UNREAD = _w.UNREAD;
                                    grp[tIdx] = wItem;
                                }
                            }
                        }
                    }
                }

                // news
                IEnumerable<NewsItems> news =
                    from item in r.NEWS
                    group item by item.DATE_D.Date into newsGroup
                    select new NewsItems(newsGroup)
                    {
                        Date = newsGroup.Key
                    };
                var cvsNews = (CollectionViewSource)Application.Current.Resources["srcNews"];
                cvsNews.Source = new NewsDateGroup(news);

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
