using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using System.ServiceModel.Web;
using System.ServiceModel;
using Nancy.Json;
using Nancy.Authentication.Forms;
using Nancy.Extensions;
using System.Dynamic;
using Nancy.Security;
using SmartLock.Models;
using Nancy.Responses;
using System.IO;
using System.Data;
using System.Globalization;
using Nancy.ModelBinding;

namespace SmartLock
{
    public class AdminWebApplication : NancyModule
    {

        public AdminWebApplication()
        {

            Get["/"] = args => {
                return new RedirectResponse("/users");
            };

            Get["/login"] = args =>
            {
                dynamic model = new ExpandoObject();
                model.Errored = this.Request.Query.error.HasValue;

                return View["login", model];
            };

            Post["/login"] = args => {
                var userGuid = AdminDatabase.ValidateUser((string)this.Request.Form.Username, (string)this.Request.Form.Password);

                if (userGuid == null)
                {
                    return this.Context.GetRedirect("~/login?error=true&username=" + (string)this.Request.Form.Username);
                }

                DateTime? expiry = null;
                if (this.Request.Form.RememberMe.HasValue)
                {
                    expiry = DateTime.Now.AddDays(7);
                }

                return this.LoginAndRedirect(userGuid.Value, expiry);
            };

            Get["/logout"] = args => {
                return this.LogoutAndRedirect("~/");
            };


            Get["/locks"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                var model = new AdminModel("locks", myUserIdentity.AdminData.AdminID, myUserIdentity.AdminData.AdminName, null, 0);

                return View["adminlocks.cshtml", model];
            };

            Get["/admins"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                var model = new AdminModel("admins", myUserIdentity.AdminData.AdminID, myUserIdentity.AdminData.AdminName, null, 0);

                return View["adminadmins.cshtml", model];
            };

            Get["/logs"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                var model = new AdminModel("logs", myUserIdentity.AdminData.AdminID, myUserIdentity.AdminData.AdminName, null, 0);

                return View["adminlogs.cshtml", model];
            };

            Get["/users"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity) this.Context.CurrentUser;

                int nlocks = 0;

                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = AdminDatabase.getLocks();
                if (myLockList == null)
                    nlocks = 0;
                else
                    nlocks = myLockList.Count();

                string[,] myLocksName = new string[nlocks,2];
                int i = 0;
                foreach (SmartLockDatabaseDataSet.Table_LocksRow lockrow in myLockList)
                {
                    myLocksName[i, 0] = lockrow.LockID.ToString();
                    myLocksName[i, 1] = lockrow.IsLockNameNull() ? lockrow.LockID.ToString() : lockrow.LockName;
                    i++;
                }

                var model = new AdminModel("users", myUserIdentity.AdminData.AdminID, myUserIdentity.AdminData.AdminName, myLocksName, nlocks);
                return View["adminuser.cshtml", model];
            };

            Get["/files/{filename}"] = args => {
                string path = "views\\AdminWebApplication\\files\\" + args.filename;
                if (!File.Exists(path)) throw new FileNotFoundException();
                return Response.AsFile(path);
            };


            Get["api/users/"] = args => {
                this.RequiresAuthentication();

                
                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_UserRow> myUserList = AdminDatabase.getUsers();

                List<AdminUserModel> myUserListBuilder = new List<AdminUserModel>();
                if (myUserList != null)
                {
                    foreach (SmartLockDatabaseDataSet.Table_UserRow userRow in myUserList)
                    {
                        string perm = AdminDatabase.getPermissions(userRow.UserID);
                        if (perm != null)
                        {
                            perm=perm.Replace(" ", "");
                        }
                        myUserListBuilder.Add(new AdminUserModel { user_id = userRow.UserID, user_name = userRow.UserName, user_surname= userRow.UserSurname, user_address = (userRow.IsUserAddressNull()?null:userRow.UserAddress), user_city = (userRow.IsUserCityNull() ? null : userRow.UserCity), user_region = (userRow.IsUserRegionNull() ? null : userRow.UserRegion), user_postalcode = (userRow.IsUserPostalCodeNull() ? null : userRow.UserPostalCode), user_country = (userRow.IsUserCountryNull() ? null : userRow.UserCountry), user_phone = (userRow.IsUserPhoneNull() ? null : userRow.UserPhone), user_mail = userRow.UserMail, user_cardtype = (userRow.IsUserCardTypeNull() ? null : userRow.UserCardType), user_cardid = (userRow.IsUserCardIDNull() ? null : userRow.UserCardID), user_pinstart = userRow.UserPinStart.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), user_pin = userRow.UserPin, user_pinexpire = userRow.UserPinExpire.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), user_registrationdate = userRow.UserRegistrationDate.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), user_lastaccess = (userRow.IsUserLastAccessNull()?null:userRow.UserLastAccess.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)), user_cardenable = userRow.UserCardEnable, user_allowedlocks = (perm==null)?"":perm });
                    }
                }

                AdminUserListModel myUserListModel = new AdminUserListModel { data = myUserListBuilder };

                Response response = Response.AsJson(myUserListModel);
                response.ContentType = "application/json";
                return response;
            };

            Post["api/users/"] = args => {
                this.RequiresAuthentication();
                var myUserModel = this.Bind<AdminUserModel>();

                string result = AdminDatabase.CreateNewUser(myUserModel);

                return result;
            };

            Put["api/users/"] = args => {
                this.RequiresAuthentication();
                var myUserModel = this.Bind<AdminUserModel>();

                string result = AdminDatabase.UpdateUser(myUserModel);

                return result;
            };

            Delete["api/users/{id}"] = args => {
                this.RequiresAuthentication();

                int parsedId = 0;
                try
                {
                    parsedId = Int32.Parse(args.id);
                }
                catch
                {
                    return "id not parsed correctly";
                }

                string result = AdminDatabase.DeleteUser(parsedId);

                return result;
            };


            Get["api/locks/"] = args => {
                this.RequiresAuthentication();


                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = AdminDatabase.getLocks();

                List<AdminLockModel> myLockListBuilder = new List<AdminLockModel>();
                if (myLockList != null)
                {
                    foreach (SmartLockDatabaseDataSet.Table_LocksRow lockRow in myLockList)
                    {
                        myLockListBuilder.Add(new AdminLockModel {id = lockRow.Id, lock_id = lockRow.LockID, lock_name = (lockRow.IsLockNameNull() ? null : lockRow.LockName), lock_enable=lockRow.LockEnabled, lock_lastseen = (lockRow.IsLockLastSeenNull() ? null : lockRow.LockLastSeen.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)), lock_minutesoffline = lockRow.LockMinutesOffline });
                    }
                }

                AdminLockListModel myLockListModel = new AdminLockListModel { data = myLockListBuilder };

                Response response = Response.AsJson(myLockListModel);
                response.ContentType = "application/json";
                return response;
            };

            Post["api/locks/"] = args => {
                this.RequiresAuthentication();
                var myLockModel = this.Bind<AdminLockModel>();

                string result = AdminDatabase.CreateNewLock(myLockModel);

                return result;
            };

            Put["api/locks/"] = args => {
                this.RequiresAuthentication();
                var myLockModel = this.Bind<AdminLockModel>();

                string result = AdminDatabase.UpdateLock(myLockModel);

                return result;
            };

            Delete["api/locks/{id}"] = args => {
                this.RequiresAuthentication();

                int parsedId = 0;
                try
                {
                    parsedId = Int32.Parse(args.id);
                }
                catch
                {
                    return "id not parsed correctly";
                }

                string result = AdminDatabase.DeleteLock(parsedId);

                return result;
            };



            Get["api/admins/"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> myAdminList = AdminDatabase.getAdmins();

                List<AdminAdminModel> myAdminListBuilder = new List<AdminAdminModel>();
                if (myAdminList != null)
                {
                    foreach (SmartLockDatabaseDataSet.Table_AdminRow adminRow in myAdminList)
                    {
                        myAdminListBuilder.Add(new AdminAdminModel { admin_id = adminRow.AdminID, admin_name = adminRow.AdminName, admin_surname = adminRow.AdminSurname, admin_login = adminRow.AdminLogin, admin_registrationdate = adminRow.AdminRegistrationDate.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), admin_phone = adminRow.AdminPhone, admin_loginfo = ((adminRow.AdminLogType / 2) % 2), admin_logaccess = (adminRow.AdminLogType % 2), admin_logerror = ((adminRow.AdminLogType / 4) % 2), admin_password = ((myUserIdentity.AdminData.AdminID == adminRow.AdminID)? adminRow.AdminPassword : null) });
                    }
                }

                AdminAdminListModel myAdminListModel = new AdminAdminListModel { data = myAdminListBuilder };

                Response response = Response.AsJson(myAdminListModel);
                response.ContentType = "application/json";
                return response;
            };

            Post["api/admins/"] = args => {
                this.RequiresAuthentication();
                var myAdminModel = this.Bind<AdminAdminModel>();

                string result = AdminDatabase.CreateNewAdmin(myAdminModel);

                return result;
            };

            Put["api/admins/"] = args => {
                this.RequiresAuthentication();
                var myAdminModel = this.Bind<AdminAdminModel>();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                myAdminModel.admin_id = myUserIdentity.AdminData.AdminID;

                string result = AdminDatabase.UpdateAdmin(myAdminModel);

                return result;
            };

            Delete["api/admins/{id}"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                int parsedId = 0;
                try
                {
                    parsedId = Int32.Parse(args.id);
                }
                catch
                {
                    return "id not parsed correctly";
                }

                string result = AdminDatabase.DeleteAdmin(parsedId);

                return result;
            };


            Get["api/logs/"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity)this.Context.CurrentUser;

                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LogRow> myLogList = AdminDatabase.getLogs();

                List<AdminLogModel> myLogListBuilder = new List<AdminLogModel>();
                if (myLogList != null)
                {
                    foreach (SmartLockDatabaseDataSet.Table_LogRow logRow in myLogList)
                    {
                        char t;
                        switch (logRow.LogType)
                        {
                            case 1: t = 'A'; break;
                            case 2: t = 'I'; break;
                            case 4: t = 'E'; break;
                            default: t = 'U'; break;
                        }

                        myLogListBuilder.Add(new AdminLogModel { log_id = logRow.LogID, log_text = logRow.LogText, log_type = t, log_source = ((logRow.LogLockID==0)?"System": ("Lock ID: "+logRow.LogLockID)), log_date= logRow.LogDate.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) });
                    }
                }

                AdminLogListModel myLogListModel = new AdminLogListModel { data = myLogListBuilder };

                Response response = Response.AsJson(myLogListModel);
                response.ContentType = "application/json";
                return response;
            };

            Delete["api/logs/"] = args => {
                this.RequiresAuthentication();
                var myLogListModel = this.Bind<AdminLogListModel>();

                string result = AdminDatabase.DeleteLogs(myLogListModel);

                return result;
            };

        }



    }
}
