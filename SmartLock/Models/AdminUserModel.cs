using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminUserModel
    {
        public int user_id { get; set; }
        public string user_name { get; set; }
        public string user_surname { get; set; }
        public string user_address { get; set; }
        public string user_city { get; set; }
        public string user_region { get; set; }
        public string user_postalcode { get; set; }
        public string user_country { get; set; }
        public string user_phone { get; set; }
        public string user_mail { get; set; }
        public string user_cardtype { get; set; }
        public string user_cardid { get; set; }
        public string user_pinstart { get; set; }
        public string user_pin { get; set; }
        public string user_pinexpire { get; set; }
        public string user_registrationdate { get; set; }
        public string user_lastaccess { get; set; }
        public int user_cardenable { get; set; }
        public string user_allowedlocks { get; set; }
    }

    public class AdminUserListModel
    {
        public List<AdminUserModel> data { get; set; }
    }
}
