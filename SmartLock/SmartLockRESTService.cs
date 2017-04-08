using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Web;
using System.ServiceModel;
using System.Globalization;
using System.Data;

namespace SmartLock
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    public class SmartLockRESTService : ISmartLockRESTService
    {
        SmartLockDatabaseDataSet myDataSet = new SmartLockDatabaseDataSet();
        SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter myLogs = new SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter();
        SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter myLocks = new SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter();
        SmartLockDatabaseDataSetTableAdapters.Table_UserTableAdapter myUsers = new SmartLockDatabaseDataSetTableAdapters.Table_UserTableAdapter();

        public SmartLockRESTService()
        {
            myLogs.Fill(myDataSet.Table_Log);
            myLocks.Fill(myDataSet.Table_Locks);
            myUsers.Fill(myDataSet.Table_User);
        }

        public AllowedUsersForLocks GetAllowedUsers(string Id)
        {

            //Check if ID is present
            int parsedId = 0;
            try
            {
                parsedId = Int32.Parse(Id);
            }
            catch
            {
                return null;
            }
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = myLocks.GetByID(parsedId).AsEnumerable();
            if (myLockList == null)
                return null;
            if (myLockList.Count() == 0)
                return null;

            SmartLockDatabaseDataSet.Table_LocksRow myLock = myLockList.ElementAt(0);
            myLocks.UpdateLockLastSeen(DateTime.Now, myLock.Id);
            myLocks.Update(myDataSet.Table_Locks);

            List<UserForLock> myUserList = new List<UserForLock>();

            if (myLock.LockEnabled == 0)
                return new AllowedUsersForLocks { AllowedUsers = myUserList };

            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> allowedUsers = myUsers.GetAllowedUsers(DateTime.Now, parsedId).AsEnumerable();
            if (allowedUsers == null)
                return new AllowedUsersForLocks { AllowedUsers = myUserList };
            if (allowedUsers.Count() == 0)
                return new AllowedUsersForLocks { AllowedUsers = myUserList };


            foreach (SmartLockDatabaseDataSet.Table_UserRow u in allowedUsers)
            {
                myUserList.Add(new UserForLock { Pin = u.UserPin, CardID = (u.UserCardEnable == 0) ? "N" : ((u.IsUserCardIDNull()) ? null : u.UserCardID), Expire = u.UserPinExpire.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) });
            }


            AllowedUsersForLocks myAllowedUsers = new AllowedUsersForLocks { AllowedUsers = myUserList };
            return myAllowedUsers;
        }

        public string ReceiveLogs(Logs data, string id)
        {
            //Check if ID is present
            int parsedId = 0;
            try
            {
                parsedId = Int32.Parse(id);
            }
            catch
            {
                return "Error: ID is not a number";
            }
            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList= myLocks.GetByID(parsedId).AsEnumerable();
            if (myLockList == null)
                return "Error: ID not present";
            if (myLockList.Count() == 0)
                return "Error: ID not present";

            SmartLockDatabaseDataSet.Table_LocksRow myLock = myLockList.ElementAt(0);
            myLocks.UpdateLockLastSeen(DateTime.Now, myLock.Id);
            myLocks.Update(myDataSet.Table_Locks);

            if (data == null)
                return "Error: Payload was empty";
            if (data.Log == null)
                return "Error: JSON not correct";

            //Check Data
            foreach (SingleLog mlog in data.Log)
            {
                if (!(mlog.Type == 1 || mlog.Type == 2 || mlog.Type == 4))
                    return "Error: Wrong Type specified (1, 2 or 4 allowed)";
                if (mlog.Type == 1 && mlog.Pin == null && mlog.CardID == null)
                    return "Error: For Type 'Access' Pin or CardID must be specified";
                if (mlog.DateTime == null)
                    return "Error: DateTime not specified";
            }

            //Create logs to store in database
            List<DatabaseLog> prepareLogs = new List<DatabaseLog>();
            int i = 0;
            foreach (SingleLog mlog in data.Log)
            {
                i++;
                string myText = "";
                if (myLock.LockName == null)
                    myText = "[" + myLock.LockID + ": ";
                else
                    myText = "[" + myLock.LockName + ": ";

                if (mlog.Type == 1)
                {
                    if (mlog.CardID == null && mlog.Pin != null)
                    {
                        EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = myUsers.GetByPin(mlog.Pin).AsEnumerable();
                        if (myUserList == null)
                            return "Error log "+i+": User not present";
                        if (myUserList.Count() == 0)
                            return "Error log " + i + ": User not present";
                        myText += "Access of "+ myUserList.ElementAt(0).UserName + " " + myUserList.ElementAt(0).UserSurname  + " using Pin] ";
                    }
                    else if (mlog.Pin == null && mlog.CardID != null)
                    {
                        EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = myUsers.GetByCardID(mlog.CardID).AsEnumerable();
                        if (myUserList == null)
                            return "Error log " + i + ": User not present";
                        if (myUserList.Count() == 0)
                            return "Error log " + i + ": User not present";
                        myText += "Access of " + myUserList.ElementAt(0).UserName + " " + myUserList.ElementAt(0).UserSurname + " using Card] ";
                    }
                    else
                    {
                        EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = myUsers.GetByPin(mlog.Pin).AsEnumerable();
                        if (myUserList == null)
                            return "Error log " + i + ": User not present";
                        if (myUserList.Count() == 0)
                            return "Error log " + i + ": User not present";

                        myUsers.UpdateCardIDbyPin(mlog.CardID, mlog.Pin);
                        

                        myText += "Access of " + myUserList.ElementAt(0).UserName + " " + myUserList.ElementAt(0).UserSurname + " using Pin and CardID added] ";
                    }

                    DateTime parsedAccess;
                    try
                    {
                        parsedAccess = DateTime.ParseExact(mlog.DateTime, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "Error log " + i + ": DateTime incorrect format. must be dd/MM/yyyy HH:mm:ss";
                    }
                    myUsers.UpdateLastAccessbyPin(parsedAccess, mlog.Pin);
                    myUsers.Update(myDataSet.Table_User);


                }
                else if(mlog.Type == 2)
                    myText += "Info] ";
                else
                    myText += "Error] ";


                DateTime parsedDateTime;
                try
                {
                    parsedDateTime = DateTime.ParseExact(mlog.DateTime, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                }
                catch
                {
                    return "Error log " + i + ": DateTime incorrect format. must be dd/MM/yyyy HH:mm:ss";
                }

                myText += "(" + mlog.DateTime + ")";
                if(mlog.Text!=null)
                    myText += " "+ mlog.Text;

                prepareLogs.Add(new DatabaseLog { Type = mlog.Type, Text = myText, ID=myLock.LockID , DateTime = parsedDateTime });
            }

            foreach(DatabaseLog l in prepareLogs)
            {
                myLogs.Insert(l.Text, l.DateTime, l.Type, l.ID);
            }

            return "OK!";
        }


        public class AllowedUsersForLocks
        {
            public List<UserForLock> AllowedUsers { get; set; }
        }
        public class UserForLock
        {
            public string Pin { get; set; }
            public string CardID { get; set; }
            public string Expire { get; set; }
        }


        public class Logs
        {
            public List<SingleLog> Log { get; set; }
        }
        public class SingleLog
        {
            public int Type { get; set; }
            public string Pin { get; set; }
            public string CardID { get; set; }
            public string Text { get; set; }
            public string DateTime { get; set; }
        }
        public class DatabaseLog
        {
            public int Type { get; set; }
            public int ID { get; set; }
            public string Text { get; set; }
            public DateTime DateTime { get; set; }
        }
    }
}
