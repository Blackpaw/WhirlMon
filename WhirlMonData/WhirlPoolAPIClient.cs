using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace WhirlMonData
{
    public class WhirlPoolAPIClient
    {
        static public String APIKey { get; set; }

        static public int GetOurId()
        {
            String[] x = WhirlMonData.WhirlPoolAPIClient.APIKey.Split('-');
            if (x.Length > 0)
            {
                int id = -1;
                int.TryParse(x[0], out id);
                return id;
            }
            else
                return -1;
        }


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

        static public bool ShowDebugToasts { get; set; }

        // Config Stuff
        static public void LoadConfig()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey("apikey"))
                APIKey = ((string)roamingSettings.Values["apikey"]).Trim();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("unreadonly"))
                UnReadOnly = (Boolean)localSettings.Values["unreadonly"];
            if (localSettings.Values.ContainsKey("ignoreown"))
                IgnoreOwnPosts = (Boolean)localSettings.Values["ignoreown"];
            if (localSettings.Values.ContainsKey("showdebugtoasts"))
                ShowDebugToasts = (Boolean)localSettings.Values["showdebugtoasts"];
            else
                ShowDebugToasts = false;
        }

        static public void SaveConfig()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["apikey"] = APIKey;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["unreadonly"] = UnReadOnly;
            localSettings.Values["ignoreown"] = IgnoreOwnPosts;
            localSettings.Values["showdebugtoasts"] = ShowDebugToasts;
        }





        static private String APIUrl(String contentType)
        {
            return String.Format("http://whirlpool.net.au/api/?key={0}&get={1}&output=json", APIKey, contentType);
        }

        static public async Task GetWatchedAsync()
        {
            await GetDataAsync(EWhirlPoolData.wpWatched);
        }

        public enum EWhirlPoolData { wpAll, wpWatched, wpNews, wpRecent }

        // Track state of unread threads
        private class ThreadReadState : Dictionary<int, int> { }
        static ThreadReadState currentThreadState = new ThreadReadState();

        static public Func<WhirlPoolAPIData.RootObject, bool> UpdateUI { get; set; }

        static public async Task GetDataAsync(EWhirlPoolData dataReq = EWhirlPoolData.wpAll)
        {
            if (APIKey == "")
                return;
            try
            {
                String ds;
                switch (dataReq)
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

                    // Preprocess
                    data.totalUnread = 0;
                    for (int i = data.WATCHED.Count - 1; i >= 0; i--)
                    {
                        WhirlMonData.WhirlPoolAPIData.WATCHED w = data.WATCHED[i];

                        if (IgnoreOwnPosts && w.LAST.ID == GetOurId())
                        {
                            data.WATCHED.RemoveAt(i);
                            continue;
                        }
                        data.totalUnread += w.UNREAD;
                    }

                    UpdateWatchedBadge(data);
                    await UpdateWatchedToasts(data.WATCHED);

                    if (UpdateUI != null)
                        UpdateUI(data);
                }


            }
            catch (Exception x)
            {
                ShowToast("GetWatched:" + x.Message);
            }
        }

        static public async Task MarkThreadReadAsync(int id, bool issueRefresh)
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
            catch (Exception x)
            {
                ShowToast("MarkThreadRead: " + x.Message);
            }
        }

        static public void ClearToast()
        {
            ToastNotificationManager.History.Remove("1", "general");
        }

        static public void ShowErrorToast(string text, Exception x)
        {
            ShowDebugToast(text + ": " + x.Message);
        }

        static public void ShowDebugToast(string toastText)
        {
            if (!ShowDebugToasts)
                return;

            ToastNotifier tn = ToastNotificationManager.CreateToastNotifier();

            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(toastText));
            ToastNotification toast = new ToastNotification(toastXml);
            tn.Show(toast);
        }

        static public void ShowToast(string toastText)
        {
            ToastNotifier tn = ToastNotificationManager.CreateToastNotifier();

            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(toastText));
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Tag = "1";
            toast.Group = "general";
            tn.Show(toast);
        }

        // Watched Detail


        static public void ShowWatchedToast(WhirlPoolAPIData.WATCHED w)
        {
            // Only if UI is not active
            if (UpdateUI != null)
                return;

            var title = w.TITLE_DECODED;
            var unread = w.UNREAD;
            var name = w.LAST.NAME;
            var id = w.ID;
            // arguments
            var lastpage = w.LASTPAGE;
            var lastread = w.LASTREAD;

            var toastXML = $@"
                            <toast activationType='foreground' launch='{id},{lastpage},{lastread}'>
                              <visual>
                                <binding template='ToastGeneric'>
                                  <text>{title} ({unread})</text>
                                  <text>{name}</text>
                                </binding>
                              </visual>
                              <actions>
                                <action activationType='background' content='Mark Read' arguments='{id}'/>
                                <action activationType='foreground' content='View' arguments='{id},{lastpage},{lastread}'/> 
                              </actions>
                            </toast>";

            var xml = new XmlDocument();
            xml.LoadXml(toastXML);

            String text = String.Format("{0}, {1} - {2}", w.TITLE_DECODED, w.UNREAD, w.LAST.NAME);
            ToastNotifier tn = ToastNotificationManager.CreateToastNotifier();

            ToastNotification toast = new ToastNotification(xml);
            toast.Tag = w.ID.ToString();
            tn.Show(toast);
        }

        static public void ClearWatchedToast(WhirlPoolAPIData.WATCHED w)
        {
            ToastNotificationManager.History.Remove(w.ID.ToString());
        }

        static public void ClearWatchedToast(int watchedId)
        {
            ToastNotificationManager.History.Remove(watchedId.ToString());
        }

        static public void ClearWatchedToast(ToastNotification tn)
        {
            ToastNotificationManager.History.Remove(tn.Tag, tn.Group);
        }

        public class ToastItem
        {
            public int id;
            public string last;

            public ToastItem(int _id, string _last)
            {
                id = _id;
                last = _last;
            }
        }

        class ToastedDictionary : Dictionary<int, string> { }

        static private async Task<ToastedDictionary> ReadToasted()
        {

            try
            {
                Windows.Storage.StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile toastedFile = await temporaryFolder.GetFileAsync("toasted.json");
                if (!toastedFile.IsAvailable)
                    return new ToastedDictionary();

                String json = await FileIO.ReadTextAsync(toastedFile);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter wr = new StreamWriter(ms))
                    {
                        wr.Write(json);
                        wr.Flush();
                        ms.Position = 0;
                        var serializer = new DataContractJsonSerializer(typeof(ToastedDictionary));
                        ToastedDictionary td = (ToastedDictionary)serializer.ReadObject(ms);
                        return td;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                return new ToastedDictionary();
            }
            catch (Exception x)
            {
                ShowToast("ReadToasted:" + x.Message);
                return new ToastedDictionary();
            }

        }

        static private async Task WriteToasted(ToastedDictionary toasted)
        {
            try
            {
                // Convert to json
                MemoryStream ms = new MemoryStream();
                var serializer = new DataContractJsonSerializer(typeof(ToastedDictionary));
                serializer.WriteObject(ms, toasted);
                ms.Flush();
                ms.Position = 0;
                var sr = new StreamReader(ms);
                String json = sr.ReadToEnd();

                // Write To File
                Windows.Storage.StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile toastedFile = await temporaryFolder.CreateFileAsync("toasted.json", CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(toastedFile, json);


            }
            catch (Exception x)
            {
                ShowToast("WriteToasted:" + x.Message);
            }
        }

        static public void UpdateWatchedBadge(WhirlMonData.WhirlPoolAPIData.RootObject data)
        {
            try
            {
                int unread = data.totalUnread;

                if (unread == 0)
                    BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                else
                {
                    XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);

                    XmlElement badgeElement = (XmlElement)badgeXml.SelectSingleNode("/badge");
                    badgeElement.SetAttribute("value", unread.ToString());

                    BadgeNotification badge = new BadgeNotification(badgeXml);
                    BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badge);
                }
                
            }
            catch (Exception x)
            {
                ShowErrorToast("UpdateWatchedBadge", x);
            }
        }
        static public async Task UpdateWatchedToasts(List<WhirlMonData.WhirlPoolAPIData.WATCHED> watched)
        {
            try
            {
                ToastedDictionary toasted = await ReadToasted();

                // debug
                // toasted.Clear();

                // Remove obsoleted toasts - iterate over toasted
                List<int> keysToRemove = new List<int>();
                foreach (var id in toasted.Keys)
                {
                    // Find
                    var w = watched.Find(x => x.ID == id);
                    if (w == null)
                    {
                        ClearWatchedToast(id);
                        keysToRemove.Add(id);
                    }
                }
                foreach (var id in keysToRemove)
                    toasted.Remove(id);

                // add/update Toasts 
                foreach (var w in watched)
                {
                    String last;
                    if (toasted.TryGetValue(w.ID, out last))
                    {
                        // see if updated
                        if (w.LAST_DATE != last)
                        {
                            ShowWatchedToast(w);
                            toasted[w.ID] = w.LAST_DATE;
                        }
                    }
                    else
                    {
                        // new
                        ShowWatchedToast(w);
                        toasted[w.ID] = w.LAST_DATE;
                    }
                }

                await WriteToasted(toasted);
            }
            catch (Exception x)
            {
                ShowErrorToast("UpdateWatchedToasts", x);
            }
        }
    }
}
