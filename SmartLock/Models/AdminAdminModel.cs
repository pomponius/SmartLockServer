using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminAdminModel
    {
        public int admin_id { get; set; }
        public string admin_name { get; set; }
        public string admin_surname { get; set; }
        public string admin_login { get; set; }
        public string admin_password { get; set; }
        public string admin_registrationdate { get; set; }
        public string admin_phone { get; set; }
        public int admin_loginfo { get; set; }
        public int admin_logaccess { get; set; }
        public int admin_logerror { get; set; }
        public int admin_logservicestate { get; set; }
    }

    public class AdminAdminListModel
    {
        public List<AdminAdminModel> data { get; set; }
    }
}