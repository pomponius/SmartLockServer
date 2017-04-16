using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminLockModel
    {
        public int id { get; set; }
        public int lock_id { get; set; }
        public string lock_name { get; set; }
        public int lock_enable { get; set; }
        public string lock_lastseen { get; set; }
        public int lock_minutesoffline { get; set; }
    }

    public class AdminLockListModel
    {
        public List<AdminLockModel> data { get; set; }
    }
}