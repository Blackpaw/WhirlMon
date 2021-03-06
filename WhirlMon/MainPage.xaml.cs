﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Windows.Input;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WhirlMonData;
using Windows.ApplicationModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WhirlMonApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        static private SynchronizationContext synchronizationContext;

        static Timer tmRefresh = null;

        public MainPage()
        {
            this.InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;

            // Uodate UI Handler
            WhirlMonData.WhirlPoolAPIClient.UpdateUI = UpdateUIData;

            // Roaming Data
            InitHandlers();

            // Update timer
            tmRefresh = new Timer(TimerRefresh, this, 1000, 1000 * 60 * 5);

            Window.Current.VisibilityChanged += Current_VisibilityChanged;


            if (CFG_UnReadOnly)
                lvWatchedHeader.Header = "Unread";
            else
                lvWatchedHeader.Header = "All";


            ShowHome();
        }

        string CFG_Version
        {
            get
            {
                Package package = Package.Current;
                PackageVersion pv = package.Id.Version;
                String ApplicationVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

                return ApplicationVersion;

            }
        }

        string CFG_APIKey
        {
            get { return WhirlMonData.WhirlPoolAPIClient.APIKey; }
            set
            {
                WhirlPoolAPIClient.APIKey = value.Trim();
                WhirlMonData.WhirlPoolAPIClient.SaveConfig();
                var t = WhirlPoolAPIClient.GetDataAsync();
            }
        }

        bool CFG_UnReadOnly
        {
            get { return WhirlPoolAPIClient.UnReadOnly; }
            set
            {
                WhirlPoolAPIClient.UnReadOnly = value;
                WhirlMonData.WhirlPoolAPIClient.SaveConfig();
                var t = WhirlPoolAPIClient.GetWatchedAsync();
                if (value)
                    lvWatchedHeader.Header = "Unread";
                else
                    lvWatchedHeader.Header = "All";
            }
        }

        bool CFG_IgnoreOwnPosts
        {
            get { return WhirlPoolAPIClient.IgnoreOwnPosts; }
            set
            {
                WhirlPoolAPIClient.IgnoreOwnPosts = value;
                WhirlMonData.WhirlPoolAPIClient.SaveConfig();
                var t = WhirlPoolAPIClient.GetWatchedAsync();
            }
        }

        bool CFG_ShowDebugToasts
        {
            get { return WhirlPoolAPIClient.ShowDebugToasts; }
            set
            {
                WhirlPoolAPIClient.ShowDebugToasts = value;
                WhirlMonData.WhirlPoolAPIClient.SaveConfig();
            }
        }


        void InitHandlers()
        {
            Windows.Storage.ApplicationData.Current.DataChanged +=
               new TypedEventHandler<ApplicationData, object>(DataChangeHandler);

            Windows.Storage.ApplicationDataContainer roamingSettings =
                Windows.Storage.ApplicationData.Current.RoamingSettings;
        }

        void DataChangeHandler(Windows.Storage.ApplicationData appData, object o)
        {
            DoRefresh();
        }

        private void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (e.Visible)
            {
                WhirlPoolAPIClient.ClearToast();
                if (WhirlPoolAPIClient.APIKey == "")
                    ShowConfig();
            }
        }
        

        private void TimerRefresh(object o)
        {
            DoRefresh();
        }

        public async void DoRefresh()
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                pbNetwork.Visibility = Visibility.Visible;
                pbNetwork.IsIndeterminate = true;
                bnRefresh.IsEnabled = false;
            }), null);

            try
            {
                await WhirlPoolAPIClient.GetDataAsync();
            }
            finally
            {
                synchronizationContext.Post(new SendOrPostCallback(o =>
                {
                    pbNetwork.Visibility = Visibility.Collapsed;
                    pbNetwork.IsIndeterminate = false;
                    bnRefresh.IsEnabled = true;
                }), null);
            }
        }

        public class WatchedThreads : ObservableCollection<WhirlMonData.WhirlPoolAPIData.WATCHED>
        {
            public WatchedThreads(IEnumerable<WhirlMonData.WhirlPoolAPIData.WATCHED> items) : base(items)
            {
            }

            public int forumId { get; set; }
            public string Forum { get; set; }
        }

        public class ThreadForumGroups : ObservableCollection<WatchedThreads>
        {
            public ThreadForumGroups(IEnumerable<WatchedThreads> items) : base(items) { }
        }

        public class NewsItems : ObservableCollection<WhirlMonData.WhirlPoolAPIData.NEWS>
        {
            public NewsItems(IEnumerable<WhirlMonData.WhirlPoolAPIData.NEWS> items) : base(items)
            {
            }

            public DateTime Date { get; set; }
            public string DOW { get { return Date.DayOfWeek.ToString(); } }
            public string SHORTDATE { get { return WhirlMonData.PrettyDate.ToShortDate(Date); } }
        }

        public class NewsDateGroup : ObservableCollection<NewsItems>
        {
            public NewsDateGroup(IEnumerable<NewsItems> items) : base(items) { }
        }

        public class RecentThreads : ObservableCollection<WhirlPoolAPIData.RECENT>
        {
            public RecentThreads(IEnumerable<WhirlPoolAPIData.RECENT> items) : base(items)
            {
            }

            public int forumId { get; set; }
            public string Forum { get; set; }
        }

        public class RecentForumGroups : ObservableCollection<RecentThreads>
        {
            public RecentForumGroups(IEnumerable<RecentThreads> items) : base(items) { }
        }


        static public bool UpdateUIData(WhirlPoolAPIData.RootObject root)
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                var r = (WhirlPoolAPIData.RootObject) o;

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

                            // Check exisiting threads in group
                            for (var tIdx = grp.Count - 1; tIdx >= 0; tIdx--)
                            {
                                WhirlPoolAPIData.WATCHED wItem = grp[tIdx];
                                var _w = _grp.SingleOrDefault(w => w.ID == wItem.ID);
                                if (_w == null)
                                {
                                    // Remove thread
                                    grp.RemoveAt(tIdx);
                                    continue;
                                }

                                // Update?
                                if (!wItem.Equals(_w))
                                    grp[tIdx] = _w;
                            }

                            // Check for new Threads
                            for (var tIdx = _grp.Count - 1; tIdx >= 0; tIdx--)
                            {
                                WhirlPoolAPIData.WATCHED wItem = _grp[tIdx];
                                var _w = grp.SingleOrDefault(w => w.ID == wItem.ID);
                                if (_w == null)
                                {
                                    // Add thread
                                    grp.Add(wItem);
                                }
                            }
                        }

                        // Check for new Groups
                        for (int gIdx = grpWatched.Count - 1; gIdx >= 0; gIdx--)
                        {
                            WatchedThreads grp = grpWatched[gIdx];
                            var _grp = current.SingleOrDefault(g => g.forumId == grp.forumId);
                            if (_grp == null)
                            {
                                // Add
                                current.Add(grp);
                            }
                        }
                    }
                }

                // news
                if (r.NEWS != null)
                {
                    IEnumerable<NewsItems> news =
                        from item in r.NEWS
                        group item by item.DATE_D.Date into newsGroup
                        select new NewsItems(newsGroup)
                        {
                            Date = newsGroup.Key
                        };
                    var cvsNews = (CollectionViewSource)Application.Current.Resources["srcNews"];
                    cvsNews.Source = new NewsDateGroup(news);
                }

                // Recent
                if (r.RECENT != null)
                {
                    // new
                    IEnumerable<RecentThreads> recent =
                        from item in r.RECENT
                        group item by item.FORUM_NAME into threadGroup
                        select new RecentThreads(threadGroup)
                        {
                            Forum = threadGroup.Key,
                            forumId = threadGroup.ElementAtOrDefault(0).FORUM_ID
                        };

                    var grpRecent = new RecentForumGroups(recent);
                    var cvsRecent = (CollectionViewSource)Application.Current.Resources["srcRecent"];
                    cvsRecent.Source = grpRecent;
                }
                }), root);

            return true;
        }

        private async void Watched_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement fe = e.OriginalSource as FrameworkElement;
            if (fe == null)
                return;

            if (fe.DataContext is WhirlPoolAPIData.WATCHED)
            {
                WhirlPoolAPIData.WATCHED watched = (WhirlPoolAPIData.WATCHED)fe.DataContext;

                String url = string.Format(@"http://forums.whirlpool.net.au/forum-replies.cfm?t={0}&p={1}&#r{2}", watched.ID, watched.LASTPAGE, watched.LASTREAD);
                var uri = new Uri(url);
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
                if (success)
                    await WhirlPoolAPIClient.MarkThreadReadAsync(watched.ID, true);
            }
            else if (fe.DataContext is WhirlPoolAPIData.RECENT)
            {
                WhirlPoolAPIData.RECENT recent = fe.DataContext as WhirlPoolAPIData.RECENT;

                String url = string.Format(@"http://forums.whirlpool.net.au/forum-replies.cfm?t={0}&p=-1&#bottom", recent.ID);
                var uri = new Uri(url);
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else if (fe.Tag != null)
            {
                // Forum Header Click
                String url = String.Format(@"https://forums.whirlpool.net.au/forum/{0}", fe.Tag);
                var uri = new Uri(url);
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }

        private async void News_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement fe = e.OriginalSource as FrameworkElement;

            if (fe != null && (fe.DataContext is WhirlPoolAPIData.NEWS))
            {
                WhirlPoolAPIData.NEWS news = (WhirlPoolAPIData.NEWS)lvNews.SelectedItem;

                var uri = new Uri(String.Format(@"http://whirlpool.net.au/news/go.cfm?article={0}", news.ID));
                var success = await Windows.System.Launcher.LaunchUriAsync(uri);
            }

        }

        private void Refresh_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DoRefresh();
        }

        private void Hamburger_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainSplitView.IsPaneOpen = !mainSplitView.IsPaneOpen;
        }

        void ShowHome()
        {
            lbContent.Text = "Watched";
            pnMain.Visibility = Visibility.Visible;
            lvNews.Visibility = Visibility.Collapsed;
            pnConfig.Visibility = Visibility.Collapsed;
        }

        void ShowNews()
        {
            lbContent.Text = "News";
            pnMain.Visibility = Visibility.Collapsed;
            lvNews.Visibility = Visibility.Visible;
            pnConfig.Visibility = Visibility.Collapsed;
        }

        void ShowConfig()
        {
            lbContent.Text = "Config";
            pnMain.Visibility = Visibility.Collapsed;
            lvNews.Visibility = Visibility.Collapsed;
            pnConfig.Visibility = Visibility.Visible;
        }

        private void Config_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainSplitView.IsPaneOpen = false;
            ShowConfig();
        }

        private void Home_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainSplitView.IsPaneOpen = false;
            ShowHome();
        }
        private void ShowNews_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainSplitView.IsPaneOpen = false;
            ShowNews();
        }

        private void lvWatched_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement fe = e.OriginalSource as FrameworkElement;
            if (fe == null)
                return;
            if (! (fe.DataContext is WhirlPoolAPIData.WATCHED))
                return;

            foreach (var m in mnuWatched.Items)
                m.Tag = fe.DataContext;

            mnuWatched.ShowAt(lvWatched, e.GetPosition(lvWatched));
        }



        private async void mnuMarkRead_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null)
                return;

            if (fe.Tag is WhirlPoolAPIData.WATCHED)
            {
                WhirlPoolAPIData.WATCHED watched = (WhirlPoolAPIData.WATCHED)fe.Tag;

                await WhirlPoolAPIClient.MarkThreadReadAsync(watched.ID, true);
            }

        }

        private async void mnuUnsubscribe_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe == null)
                return;

            if (fe.Tag is WhirlPoolAPIData.WATCHED)
            {
                WhirlPoolAPIData.WATCHED watched = (WhirlPoolAPIData.WATCHED)fe.Tag;

                await WhirlPoolAPIClient.UnsubscribeThreadAsync(watched.ID, true);
            }
        }
    }
}
