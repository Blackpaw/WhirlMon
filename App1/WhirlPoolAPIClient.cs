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

        static private String APIUrl(String contentType)
        {
            APIKey = "79947-309525-784";

            return String.Format("http://whirlpool.net.au/api/?key={0}&get={1}&output=json", APIKey, contentType);
        }



        static public async void GetWatchedAsync(bool unreadOnly)
        {
            try
            {
                String url = APIUrl("watched") + "&watchedmode=0";

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
                // ?
            }
        }

    }
}
