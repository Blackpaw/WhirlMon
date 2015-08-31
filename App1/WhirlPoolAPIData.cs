using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace WhirlMon
{
    public class PrettyDate
    {
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        static public String ToShortDate(DateTime d)
        {
            GeographicRegion userRegion = new GeographicRegion();
            var userDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate", new[] { userRegion.Code });
            return userDateFormat.Format(d);
        }

        static public String Get(DateTime yourDate)
        {
            var ts = new TimeSpan(DateTime.Now.Ticks - yourDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }
            if (delta < 2 * MINUTE)
            {
                return "a minute ago";
            }
            if (delta < 45 * MINUTE)
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 90 * MINUTE)
            {
                return "an hour ago";
            }
            if (delta < 24 * HOUR)
            {
                return ts.Hours + " hours ago";
            }
            if (delta < 48 * HOUR)
            {
                return "yesterday";
            }
            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }
            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
    public class WhirlPoolAPIData
    {
        public class BASE
        {
            public int ID { get; set; }
            public string TITLE { get; set; }

            public string TITLE_DECODED { get { return WebUtility.HtmlDecode(TITLE); } }
        }

        public class NEWS : BASE
        {
            public string DATE { get; set; }
            public string SOURCE { get; set; }
            public string BLURB { get; set; }
            public DateTime DATE_D { get {return DateTime.Parse(DATE); } }
            public string DATE_S {  get { return PrettyDate.ToShortDate(DATE_D); } }
        }

        public class FIRST
        {
            public string NAME { get; set; }
            public int ID { get; set; }
        }

        public class LAST
        {
            public string NAME { get; set; }
            public int ID { get; set; }
        }

        public class WATCHED : BASE
        {
            public string LAST_DATE { get; set; }
            public FIRST FIRST { get; set; }
            public string FORUM_NAME { get; set; }
            public LAST LAST { get; set; }
            public int LASTREAD { get; set; }
            public int FORUM_ID { get; set; }
            public int UNREAD { get; set; }
            public int LASTPAGE { get; set; }
            public int REPLIES { get; set; }
            public string FIRST_DATE { get; set; }

            public String LAST_DATE_D
            {
                
                get
                {
                    DateTime d = DateTime.Parse(LAST_DATE);
                    return PrettyDate.Get(d) + " " + d.ToString("h:mm");
                }
            }
        }

        public class FIRST2
        {
            public string NAME { get; set; }
            public int ID { get; set; }
        }

        public class LAST2
        {
            public string NAME { get; set; }
            public int ID { get; set; }
        }

        public class RECENT : BASE
        {
            public string LAST_DATE { get; set; }
            public FIRST2 FIRST { get; set; }
            public string FORUM_NAME { get; set; }
            public LAST2 LAST { get; set; }
            public int FORUM_ID { get; set; }
            public int REPLIES_BY_USER { get; set; }
            public int REPLIES { get; set; }
            public string FIRST_DATE { get; set; }

        }

        public class RootObject
        {
            public List<NEWS> NEWS { get; set; }
            public List<WATCHED> WATCHED { get; set; }
            public List<RECENT> RECENT { get; set; }
        }
    }
}
