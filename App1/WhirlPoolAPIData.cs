using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WhirlMon
{
    public class WhirlPoolAPIData
    {
        public class NEWS
        {
            public string DATE { get; set; }
            public string SOURCE { get; set; }
            public string BLURB { get; set; }
            public int ID { get; set; }
            public string TITLE { get; set; }
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

        public class WATCHED
        {
            public string LAST_DATE { get; set; }
            public FIRST FIRST { get; set; }
            public string FORUM_NAME { get; set; }
            public LAST LAST { get; set; }
            public int LASTREAD { get; set; }
            public int FORUM_ID { get; set; }
            public int UNREAD { get; set; }
            public int ID { get; set; }
            public int LASTPAGE { get; set; }
            public string TITLE { get; set; }
            public int REPLIES { get; set; }
            public string FIRST_DATE { get; set; }

            public string TITLE_DECODED { get { return WebUtility.HtmlDecode(TITLE); }}
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

        public class RECENT
        {
            public string LAST_DATE { get; set; }
            public FIRST2 FIRST { get; set; }
            public string FORUM_NAME { get; set; }
            public LAST2 LAST { get; set; }
            public int FORUM_ID { get; set; }
            public int REPLIES_BY_USER { get; set; }
            public int ID { get; set; }
            public string TITLE { get; set; }
            public int REPLIES { get; set; }
            public string FIRST_DATE { get; set; }

            public string TITLE_DECODED { get { return WebUtility.HtmlDecode(TITLE); } }
        }

        public class RootObject
        {
            public List<NEWS> NEWS { get; set; }
            public List<WATCHED> WATCHED { get; set; }
            public List<RECENT> RECENT { get; set; }
        }
    }
}
