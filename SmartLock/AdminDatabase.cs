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
        private static SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter myLogs = new SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter();



        static AdminDatabase()
        {

            myAdmin.Fill(myDataSet.Table_Admin);
            myUsers.Fill(myDataSet.Table_User);
            myLocks.Fill(myDataSet.Table_Locks);
            myPermissions.Fill(myDataSet.Table_Permissions);
            myLogs.Fill(myDataSet.Table_Log);
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

        public static EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LogRow> getLogs()
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LogRow> myLogList = myLogs.GetData().AsEnumerable();
            if (myLogList == null)
                return null;
            if (myLogList.Count() == 0)
                return null;
            return myLogList;
        }

        public static EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> getAdmins()
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> myAdminList = myAdmin.GetData().AsEnumerable();
            if (myAdminList == null)
                return null;
            if (myAdminList.Count() == 0)
                return null;
            return myAdminList;
        }

        public static string CreateNewUser(AdminUserModel mynewuser, UserIdentity myUserIdentity)
        {
            //generate a new pin
            Random rnd = new Random();
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList;
            int newpin;
            string newpinstring;
            do {
                newpin = rnd.Next(100000);
                newpinstring = newpin.ToString("D5");
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

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " created a new user " + mynewuser.user_name  + " " + mynewuser.user_surname, DateTime.Now, 2, 0);

            return "OK!";
        }

        public static string CreateNewLock(AdminLockModel mynewlock, UserIdentity myUserIdentity)
        {
            if (mynewlock.lock_id < 1)
                return "ID must be >0";

            //check if id is already present
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = myLocks.GetByID(mynewlock.lock_id).AsEnumerable();
            if (myLockList.Count() != 0)
                return "ID already present";

            myLocks.Insert(mynewlock.lock_id, mynewlock.lock_name, mynewlock.lock_enable, null, mynewlock.lock_minutesoffline, DateTime.Now);

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " created a new lock " + mynewlock.lock_name  + " ID: " + mynewlock.lock_id, DateTime.Now, 2, 0);

            return "OK!";
        }

        public static string CreateNewAdmin(AdminAdminModel mynewadmin, UserIdentity myUserIdentity)
        {
            int log = 0;
            if (mynewadmin.admin_logaccess == 1)
                log += 1;
            if (mynewadmin.admin_loginfo == 1)
                log += 2;
            if (mynewadmin.admin_logerror == 1)
                log += 4;

            myAdmin.Insert(mynewadmin.admin_name, mynewadmin.admin_surname, mynewadmin.admin_login, mynewadmin.admin_password, DateTime.Now, mynewadmin.admin_phone, log, Guid.NewGuid().ToString(), null);

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " created a new admin " + mynewadmin.admin_name + " " + mynewadmin.admin_surname, DateTime.Now, 2, 0);

            return "OK!";
        }



        public static string UpdateUser(AdminUserModel myuser, UserIdentity myUserIdentity)
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

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " updated user " + myuser.user_name + " " + myuser.user_surname, DateTime.Now, 2, 0);

            return "OK!";
        }


        public static string UpdateLock(AdminLockModel mylock, UserIdentity myUserIdentity)
        {
            myLocks.UpdateLock(mylock.lock_name, mylock.lock_enable, mylock.lock_minutesoffline, mylock.id);

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " updated lock " + mylock.lock_name + " ID: " + mylock.lock_id, DateTime.Now, 2, 0);

            return "OK!";
        }

        public static string UpdateAdmin(AdminAdminModel myadmin, UserIdentity myUserIdentity)
        {
            int log = 0;
            if (myadmin.admin_logaccess == 1)
                log += 1;
            if (myadmin.admin_loginfo == 1)
                log += 2;
            if (myadmin.admin_logerror == 1)
                log += 4;

            myAdmin.UpdateAdmin(myadmin.admin_name, myadmin.admin_surname, myadmin.admin_login, myadmin.admin_password, myadmin.admin_phone, log, myadmin.admin_id);

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " updated his account", DateTime.Now, 2, 0);

            return "OK!";
        }


        public static string DeleteUser(int id, UserIdentity myUserIdentity)
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = myUsers.GetDataByUserID(id).AsEnumerable();
            if (myUserList == null)
                return "ID not present";
            if (myUserList.Count() == 0)
                return "ID not present";

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " deleted user " + myUserList.ElementAt(0).UserName + " " + myUserList.ElementAt(0).UserSurname, DateTime.Now, 2, 0);

            myUsers.DeleteByUserID(id);
            myPermissions.DeletePermissions(id);

            return "OK!";
        }

        public static string DeleteLock(int id, UserIdentity myUserIdentity)
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = myLocks.GetByID(id).AsEnumerable();
            if (myLockList == null)
                return "ID not present";
            if (myLockList.Count() == 0)
                return "ID not present";

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " deleted lock " + myLockList.ElementAt(0).LockName + " ID: " + myLockList.ElementAt(0).LockID, DateTime.Now, 2, 0);

            myLocks.DeleteByLockID(id);
            myPermissions.DeleteLocks(id);
            return "OK!";
        }

        public static string DeleteAdmin(int id, UserIdentity myUserIdentity)
        {
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> myAdminList = myAdmin.GetData().AsEnumerable();
            if (myAdminList.Count() == 1)
                return "At leat one Admin must be present!";

            myAdminList = myAdmin.GetDataByAdminID(id).AsEnumerable();
            if (myAdminList == null)
                return "ID not present";
            if (myAdminList.Count() == 0)
                return "ID not present";

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " deleted admin " + myAdminList.ElementAt(0).AdminName + " " + myAdminList.ElementAt(0).AdminSurname, DateTime.Now, 2, 0);

            myAdmin.DeleteByAdminID(id);
            return "OK!";
        }

        public static string DeleteLogs(AdminLogListModel myloglist, UserIdentity myUserIdentity)
        {
            foreach (AdminLogModel mylog in myloglist.data)
            {
                myLogs.DeleteByLogID(mylog.log_id);
            }

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " deleted " + myloglist.data.Count + " log" + ((myloglist.data.Count==1)?"":"s"), DateTime.Now, 2, 0);

            return "OK!";
        }

        public static string BotUpdateAsync(string token, string enable, UserIdentity myUserIdentity)
        {
            string[] lines = { token, enable };
            string name="unknown";

            if (token != "")
            {
                try
                {
                    Telegram.Bot.TelegramBotClient Bot = new Telegram.Bot.TelegramBotClient(token);
                    var me = Bot.GetMeAsync();
                    name = me.Result.FirstName;
                }
                catch (Exception e)
                {
                    if (e.InnerException == null)
                        return e.Message;
                    return e.InnerException.Message;
                }
            }
            
            System.IO.File.WriteAllLines("BotTelegram.txt", lines);

            int en = Int32.Parse(enable);
            string ens = "disabled";

            if (en == 1)
                ens = "enabled";
            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + myUserIdentity.AdminData.AdminName + " " + myUserIdentity.AdminData.AdminSurname + " updated Telegram Bot configuration (Now: " + ens + "; Bot name: "+ name + ")", DateTime.Now, 2, 0);

            return "OK!";
        }



        public static Guid? ValidateUser(string username, string password)
        {

            
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> Rows = myAdmin.GetAdminFromLogin(username, password).AsEnumerable();
            if (Rows == null)
                return null;
            if (Rows.Count() == 0)
                return null;

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Admin " + Rows.ElementAt(0).AdminName+" "+ Rows.ElementAt(0).AdminSurname + " connected to the management web service", DateTime.Now, 2, 0);

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
