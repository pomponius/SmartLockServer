using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminModel
    {
        public int AdminID { get; private set; }
        public string Adminname { get; private set; }
        public string[,] LocksName { get; private set; }
        public int NLocks { get; private set; }
        public string Page { get; private set; }

        public AdminModel( string page, int id, string username, string[,] locks, int nlocks)
        {
            AdminID = id;
            Page = page;
            Adminname = username;
            LocksName = locks;
            NLocks = nlocks;
        }
    }
}
