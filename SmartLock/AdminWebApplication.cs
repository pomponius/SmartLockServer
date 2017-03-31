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


namespace SmartLock
{
    public class AdminWebApplication : NancyModule
    {

        private class Table
        {
            public string Name { get; set; }
            public int Age { get; set; }

        }


        private class Product
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int Id { get; set; }
            public decimal Price { get; set; }

        }

        Product[] products = new Product[]
        {
            new Product { Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1 },
            new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M },
            new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M }
        };


        public AdminWebApplication()
        {
            Get["/hello"] = parameters => {
                {
                    Table model = new Table();

                    model.Name = "Marco";
                    model.Age = 24;


                    return View["indexhello", model];
                };
            };

            Get["/products"] = parameters => {
                {
                    return View["products"];
                };
            };

            Get["api/products/"] = parameters =>
            {
                Response response = Response.AsJson(products);
                response.ContentType = "application/json";
                return response;
            };

            Get["api/products/{id}"] = parameters =>
            {
                Product product = products.FirstOrDefault((p) => p.Id == parameters.id);
                if (product == null)
                {
                    return "";
                }
                Response response = Response.AsJson(product);
                response.ContentType = "application/json";
                return response;
            };

            /*Get["/"] = args => {
                return View["index"];
            };*/
            Get["/"] = args => {
                return new RedirectResponse("/secure");
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


            Get["/secure"] = args => {
                this.RequiresAuthentication();

                UserIdentity myUserIdentity = (UserIdentity) this.Context.CurrentUser;
                var model = new AdminModel(myUserIdentity.AdminData.AdminName);
                return View["secure.cshtml", model];
            };
        }



    }
}
