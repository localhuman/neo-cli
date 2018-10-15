using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Neo.Notifications
{
    public class IterResult
    {
        public byte[] value;
        public byte[] key;

        private JToken _json = null;

        public JToken json
        {
            get
            {
                if (_json == null)
                {
                    _json = JToken.Parse(Encoding.Default.GetString(value));
                    _json.Last.AddAfterSelf(new JProperty("key", "0x"+key.ToHexString()));
                }
                return _json;
            }
        }
    }

    public class NotificationQuery
    {
        private const int maxPageSize = 1000;
        public int Page { get; set; } = 1;
        private int _pageSize = 500;

        public List<string> EventType { get; set; } = null;

        public int AfterBlock { get; set; } = -1;
        public int BeforeBlock { get; set; } = -1;

        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value < maxPageSize) ? value : maxPageSize;
            }
        }

    }


    public class NotificationResult
    {
        public uint current_height { get; set; }
        public string message { get; set; }
        public List<JToken> results { get; set; }

        public int page { get; set; }
        public int page_len { get; set; }
        public int total { get; set; }
        public int total_pages { get; set; }

        public void Paginate(NotificationQuery query)
        {

            total = results.Count;
            page_len = query.PageSize;
            total_pages = (total > 0) ? (int)Math.Ceiling(total / (double)page_len) : 0;
            page = query.Page;

            if( total == 0)
            {
                message = "No results";
                return;
            }

            
            if( page > total_pages || page <= 0)
            {
                message = "Invalid page";
                results = new List<JToken>();
                return;
            }

            if (total > query.PageSize)
            {
                int offset = query.PageSize * (query.Page - 1);
                int count = (offset + query.PageSize > total) ? total - offset : query.PageSize;
                results = results.OrderBy(r => r["block"]).ThenBy(r => r["index"]).ToList().GetRange(offset, count);
            }
        }
    }
}
