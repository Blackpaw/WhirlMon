using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WhirlMon
{
    public class WhirlPoolAPIClient
    {
        static public String APIKey { get; set; }

        static private bool _unreadOnly = true;
        static public bool UnReadOnly
        {
            get
            {
                return _unreadOnly;
            }
            set
            {
                _unreadOnly = value;
            }
        }


        static private bool _ignoreOwnPosts = true;
        static public bool IgnoreOwnPosts
        {
            get
            {
                return _ignoreOwnPosts;
            }
            set
            {
                _ignoreOwnPosts = value;
            }
        }


        static private String APIUrl(String contentType)
        {
            return String.Format("http://whirlpool.net.au/api/?key={0}&get={1}&output=json", APIKey, contentType);
        }

        static public async Task GetWatchedAsync()
        {
            await GetDataAsync(EWhirlPoolData.wpWatched);
        }

        public enum EWhirlPoolData {wpAll, wpWatched, wpNews, wpRecent}

        // Track state of unread threads
        private class ThreadReadState : Dictionary<int, int> {}
        static ThreadReadState currentThreadState = new ThreadReadState();

        static public async Task GetDataAsync(EWhirlPoolData dataReq = EWhirlPoolData.wpAll)
        {
            if (APIKey == "")
                return;
            try
            {
                String ds;
                switch(dataReq)
                {
                    case EWhirlPoolData.wpWatched: ds = "watched"; break;
                    case EWhirlPoolData.wpNews: ds = "news"; break;
                    case EWhirlPoolData.wpRecent: ds = "recent"; break;

                    default:
                        ds = "watched+news+recent";
                        break;
                }

                String url = APIUrl(ds);
                if (UnReadOnly && (dataReq == EWhirlPoolData.wpAll || dataReq == EWhirlPoolData.wpWatched))
                    url += "&watchedmode=0";

                var asyncClient = new HttpClient();
                asyncClient.DefaultRequestHeaders.Add("User-Agent",
                                 "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident / 6.0)");
                String json = await asyncClient.GetStringAsync(url);

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(WhirlPoolAPIData.RootObject));

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var data = (WhirlPoolAPIData.RootObject)serializer.ReadObject(ms);

                    int new_unread = 0;
                    /// Calcuate new msgs against last run
                    foreach(var watched in data.WATCHED)
                    {
                        int unread = 0;
                        if (currentThreadState.TryGetValue(watched.FORUM_ID, out unread))
                        {
                            // alread retrieved it
                            if (watched.UNREAD > unread)
                                new_unread += (watched.UNREAD - unread);
                        }
                        else
                        {
                            // new
                            new_unread += watched.UNREAD;
                        }
                        currentThreadState[watched.FORUM_ID] = watched.UNREAD;
                    }

                    // remove threads read since last run
                    List<int> fids = new List<int>();
                    foreach (var fid in currentThreadState.Keys)
                    {
                        bool exists = data.WATCHED.Exists(x => x.FORUM_ID == fid);
                        if (!exists)
                            fids.Add(fid);
                    }
                    foreach (var fid in fids)
                        currentThreadState.Remove(fid);

                    // Generate toast notification if needed
                    if (new_unread > 0)
                    {
                        string toastText = string.Format("{0} new messages", new_unread);
                        ShowToast(toastText);
                    }


                    WhirlMonApp.MainPage.UpdateUIData(data);
                }

               
            }
            catch(Exception x)
            {
                // TODO - Log error
                var dialog = new Windows.UI.Popups.MessageDialog("GetWatched:" + x.Message);
                var t = dialog.ShowAsync();
            }
        }

        static public async void MarkThreadReadAsync(int id, bool issueRefresh)
        {
            if (APIKey == "")
                return;
            try
            {
                string url = String.Format("http://whirlpool.net.au/api/?key={0}&watchedread={1}", APIKey, id);

                var asyncClient = new HttpClient();
                asyncClient.DefaultRequestHeaders.Add("User-Agent",
                                 "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident / 6.0)");
                String json = await asyncClient.GetStringAsync(url);

                if (issueRefresh)
                    await GetWatchedAsync();

            }
            catch (Exception)
            {
                // TODO - Log error
            }
        }

        /// <summary>
        /// Toast stuff
        /// </summary>
        static ToastNotifier m_tn = null;
        static ToastNotification lastToast = null;

        static public void ClearToast()
        {
            if (lastToast != null)
            {
                m_tn.Hide(lastToast);
                lastToast = null;
            }
        }

        static void ShowToast(string toastText)
        {
            if (m_tn == null)
                m_tn = ToastNotificationManager.CreateToastNotifier();

            ClearToast();

            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(toastText));
            lastToast = new ToastNotification(toastXml);
            m_tn.Show(lastToast);
        }

    }
}
