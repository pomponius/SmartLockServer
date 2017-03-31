using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Web;
using System.ServiceModel;


namespace SmartLock
{
    [ServiceContract(Name = "SmartLockRESTService")]
    public interface ISmartLockRESTService
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "data/?id={id}")]
        SmartLockRESTService.AllowedUsersForLocks GetAllowedUsers(string Id);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "data/?id={id}")]
        string ReceiveLogs(SmartLockRESTService.Logs data, string id);
    }
}
