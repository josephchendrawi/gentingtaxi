using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class BaseController : Controller
    {
        protected override void ExecuteCore()
        {
            HttpCookie Cookie = Request.Cookies["GTCookie"] ?? new HttpCookie("GTCookie");
            if (Request.Path.ToLower().Contains("/admin/login"))
            {
                if (Request.IsAuthenticated && Cookie["Email"] != null && Cookie["Name"] != null && Cookie["AdminId"] != null)
                {
                    //View("Index").ExecuteResult(ControllerContext);
                    RedirectToAction("Login", "Admin").ExecuteResult(this.ControllerContext);
                }
                else
                {
                    base.ExecuteCore();
                }
            }
            else
            {
                if (Request.IsAuthenticated && AdminService.IsAuthenticated() && Cookie["Email"] != null && Cookie["Name"] != null)
                {
                    base.ExecuteCore();
                }
                else
                {
                    //ViewBag.ReturnURL = Server.UrlEncode(Request.Url.AbsoluteUri);
                    View("Unauthenticated").ExecuteResult(ControllerContext);
                }
            }
        }

        protected override bool DisableAsyncSupport
        {
            get { return true; }
        }
    }
}