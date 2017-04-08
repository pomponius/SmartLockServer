using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Forms;
using Nancy.Security;
using Nancy;
using SmartLock.Models;
using System.Globalization;

namespace SmartLock
{
    public class AdminDatabase : IUserMapper
    {
        private static SmartLockDatabaseDataSet myDataSet = new SmartLockDatabaseDataSet();
        private static SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter myAdmin = new SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter();
        private static SmartLockDatabaseDataSetTableAdapters.Table_UserTableAdapter myUsers = new SmartLockDatabaseDataSetTableAdapters.Table_UserTableAdapter();
        private static SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter myLocks = new SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter();
        private static SmartLockDatabaseDataSetTableAdapters.Table_PermissionsTableAdapter myPermissions = new SmartLockDatabaseDataSetTableAdapters.Table_PermissionsTableAdapter();



        static AdminDatabase()
        {

            myAdmin.Fill(myDataSet.Table_Admin);
            myUsers.Fill(myDataSet.Table_User);
            myLocks.Fill(myDataSet.Table_Locks);
        }


        public  static EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> getUsers() {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = myUsers.GetData().AsEnumerable();
            if (myUserList == null)
                return null;
            if (myUserList.Count() == 0)
                return null;
            return myUserList;
        }

        public static string getPermissions(int userID)
        {
            return (string) myPermissions.GetPermissions(userID);
        }

        public static EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> getLocks()
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = myLocks.GetData().AsEnumerable();
            if (myLockList == null)
                return null;
            if (myLockList.Count() == 0)
                return null;
            return myLockList;
        }

        public static string CreateNewUser(AdminUserModel mynewuser)
        {
            //generate a new pin
            Random rnd = new Random();
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList;
            int newpin;
            string newpinstring;
            do {
                newpin = rnd.Next(100000);      // 0 <= card < 52
                newpinstring = "" + newpin;
                myUserList = myUsers.GetByPin(newpinstring).AsEnumerable();
                if (myUserList == null)
                    return "Query result is null";
            } while (myUserList.Count() != 0);

            int instertedID;
            myUsers.InsertAndReturnID(mynewuser.user_name, mynewuser.user_surname, mynewuser.user_address, mynewuser.user_city, mynewuser.user_region, mynewuser.user_postalcode, mynewuser.user_country, mynewuser.user_phone, mynewuser.user_mail, mynewuser.user_cardtype, mynewuser.user_cardid, DateTime.ParseExact(mynewuser.user_pinstart, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), newpinstring, DateTime.ParseExact(mynewuser.user_pinexpire, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTime.Now, null, mynewuser.user_cardenable, out instertedID);

            if (mynewuser.user_allowedlocks.Length > 0)
            {
                string[] userlocks = mynewuser.user_allowedlocks.Split(',');
                foreach(string ml in userlocks)
                {
                    myPermissions.Insert(instertedID, Int32.Parse(ml));
                }
            }
            

            return "OK!";
        }

        public static string UpdateUser(AdminUserModel myuser)
        {

            myUsers.UpdateUser(myuser.user_name, myuser.user_surname, myuser.user_address, myuser.user_city, myuser.user_region, myuser.user_postalcode, myuser.user_country, myuser.user_phone, myuser.user_mail, myuser.user_cardtype, myuser.user_cardid, DateTime.ParseExact(myuser.user_pinstart, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTime.ParseExact(myuser.user_pinexpire, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), myuser.user_cardenable, myuser.user_id);
            myPermissions.DeletePermissions(myuser.user_id);
            if (myuser.user_allowedlocks.Length > 0)
            {
                string[] userlocks = myuser.user_allowedlocks.Split(',');
                foreach (string ml in userlocks)
                {
                    myPermissions.Insert(myuser.user_id, Int32.Parse(ml));
                }
            }

            return "OK!";
        }

        public static string DeleteUser(int id)
        {
            myUsers.DeleteByUserID(id);
            myPermissions.DeletePermissions(id);
            return "OK!";
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
