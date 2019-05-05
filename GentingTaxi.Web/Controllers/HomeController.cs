using GentingTaxi.Models;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetOverallUsage()
        {
            OverallStatus OverallStatus = new OverallStatus();
            OverallStatus.BookingCount = new BookingService().GetBookingCount();
            OverallStatus.DriverCount = new DriverService().GetDriverCount();
            OverallStatus.UserCount = new UserService().GetUserCount();

            return Json(OverallStatus, JsonRequestBehavior.AllowGet);
        }
    }
}