using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLock.Models
{
    public class AdminModel
    {
        public string Username { get; private set; }

        public AdminModel(string username)
        {
            Username = username;
        }
    }
}
