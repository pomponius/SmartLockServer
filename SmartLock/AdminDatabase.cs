using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Forms;
using Nancy.Security;
using Nancy;

namespace SmartLock
{
    public class AdminDatabase : IUserMapper
    {
        private static SmartLockDatabaseDataSet myDataSet = new SmartLockDatabaseDataSet();
        private static SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter myAdmin = new SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter();



        static AdminDatabase()
        {

            myAdmin.Fill(myDataSet.Table_Admin);
        }




        public static Guid? ValidateUser(string username, string password)
        {

            
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> Rows = myAdmin.GetAdminFromLogin(username, password).AsEnumerable();
            if (Rows == null)
                return null;
            if (Rows.Count() == 0)
                return null;


            return new Guid(Rows.ElementAt(0).AdminGuid);
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {

            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> Rows = myAdmin.GetAdminFromGuid(identifier.ToString()).AsEnumerable();
            if (Rows == null)
                return null;
            if (Rows.Count() == 0)
                return null;

            return new UserIdentity { UserName = Rows.ElementAt(0).AdminLogin, AdminData = Rows.ElementAt(0) };
        }
    }

    public class UserIdentity : IUserIdentity
    {
        public string UserName { get; set; }
        public IEnumerable<string> Claims { get; }
    
        public SmartLockDatabaseDataSet.Table_AdminRow AdminData { get; set; }

    }

}
