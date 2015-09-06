using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

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
    }
}
