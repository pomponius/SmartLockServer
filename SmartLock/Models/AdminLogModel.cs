using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminLogModel
    {
        public int log_id { get; set; }
        public char log_type { get; set; }
        public string log_date { get; set; }
        public string log_source { get; set; }
        public string log_text { get; set; }
    }

    public class AdminLogListModel
    {
        public List<AdminLogModel> data { get; set; }
    }
}