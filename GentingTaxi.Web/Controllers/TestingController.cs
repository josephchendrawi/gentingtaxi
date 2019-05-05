using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Web.Controllers
{
    public class TestingController : Controller
    {
        DriverService DriverService = new DriverService();
        BookingService BookingService = new BookingService();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DriverLocation(string DriverIC)
        {
            try
            {
                var DriverVM = DriverService.GetDriver(DriverService.GetDriverIdByIC(DriverIC));

                Driver Model = new Driver();
                Model.DriverId = DriverVM.driverId;
                Model.Name = DriverVM.name;
                Model.IDCardNo = DriverVM.ic;
                Model.CurrentLocation = new Location()
                {
                    Latitude = DriverVM.current_lat == null ? -1 : DriverVM.current_lat.Value,
                    Longitude = DriverVM.current_lng == null ? -1 : DriverVM.current_lng.Value
                };

                return View(Model);
            }
            catch (Exception e)
            {
                TempData["Message"] = e.Message;
                return RedirectToAction("Index", "Testing");
            }
        }

        public ActionResult DriverBookings(int BookingId)
        {
            ViewBag.BookingId = BookingId;
            return View();
        }

        public ActionResult ListDriverBookings(DataTableParam param, int BookingId = 0)
        {
            try
            {
                var model = new DriverBookingsListData();

                int TotalCount = 0;

                var List = BookingService.GetDriverBookings(param.iDisplayStart, param.iDisplayLength, ref TotalCount, BookingId);

                List<DriverBookings> DriverBookingsList = new List<DriverBookings>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    DriverBookings VM = new DriverBookings();
                    VM.BookingId = v.DriverBookingsVM.bookingId;
                    VM.CreatedDate = v.DriverBookingsVM.created_date == null ? "-" : v.DriverBookingsVM.created_date.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.Driver = new Driver()
                    {
                        DriverId = v.DriverBookingsVM.driverId,
                        Name = v.DriverVM.name
                    };
                    VM.User = new User()
                    {
                        Name = v.UserVM.name
                    };
                    VM.DriverBookingsId = v.DriverBookingsVM.driverBookingsId;
                    VM.LastUpdated = v.DriverBookingsVM.last_updated == null ? "-" : v.DriverBookingsVM.last_updated.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.ResponseDateTime = v.DriverBookingsVM.response_datettime == null ? "-" : v.DriverBookingsVM.response_datettime.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.Status = v.DriverBookingsVM.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((DriverBookingStatus)v.DriverBookingsVM.status).ToString(),
                        StatusColor = ((StatusColor)v.DriverBookingsVM.status).ToString()
                    };

                    DriverBookingsList.Add(VM);
                }

                model.aaData = DriverBookingsList;
                model.iTotalDisplayRecords = TotalCount;
                model.iTotalRecords = TotalCount;
                model.sEcho = param.sEcho;

                return Json(model, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        
        public ActionResult GetNotification()
        {
            return View();
        }

        public ActionResult PushNotification()
        {
            GentingTaxiApi.Service.Helper.PushNotificationToWeb("New Booking Transaction Created with Booking ID : 1");
            return null;
        }

    }
}
