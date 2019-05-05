using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class BookingTrxController : BaseController
    {
        BookingService BookingService = new BookingService();
        DriverService DriverService = new DriverService();

        public ActionResult List()
        {
            return View();
        }

        public ActionResult ListBookingTrx(DataTableParam param, int UserId = 0, int DriverId = 0, int ManualAssign = 0, string User = "", string Driver = "", string BookingDateStart = "", string BookingDateTo = "", string Status = "")
        {
            try
            {
                var model = new BookingTrxListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 0 ? "BookingId" : 
                                            sortColumnIndex == 1 ? "UserName" :
                                            sortColumnIndex == 2 ? "DriverName" :
                                            sortColumnIndex == 3 ? "BookingDateTime" :
                                            sortColumnIndex == 4 ? "From" :
                                            sortColumnIndex == 5 ? "To" :
                                            sortColumnIndex == 6 ? "Status" :
                                            "BookingDateTime";

                var sortDirection = Request["sSortDir_0"]; // asc or desc
                
                var List = BookingService.GetAllBookings(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, ManualAssign == 1 ? true : false, UserId, DriverId, User, Driver, BookingDateStart, BookingDateTo, Status);
                
                List<BookingTrx> BookingTrxList = new List<BookingTrx>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    BookingTrx VM = new BookingTrx();
                    VM.BookingTrxId = v.BookingVM.bookingId;
                    VM.UserName = v.UserVM.name;
                    VM.DriverName = v.DriverVM.name;
                    VM.BookingDateTimeText = v.BookingVM.booking_datetime == null ? "-" : v.BookingVM.booking_datetime.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.CreatedDateText = v.BookingVM.created_date == null ? "-" : v.BookingVM.created_date.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.FromLocation = v.BookingVM.from;
                    VM.ToLocation = v.BookingVM.to;
                    VM.Status = v.BookingVM.booking_status;

                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((BookingStatus)v.BookingVM.booking_status).ToString(),
                        StatusColor = ((StatusColor)v.BookingVM.booking_status).ToString()
                    };

                    BookingTrxList.Add(VM);
                }

                model.aaData = BookingTrxList;
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
        
        public ActionResult View(int Id)
        {
            BookingTrx model = new BookingTrx();

            var result = BookingService.GetBooking(Id);

            model.User = new User()
            {
                UserId = result.UserVM.userId,
                Name = result.UserVM.name,
                Username = result.UserVM.username,
                Email = result.UserVM.email,
                Phone = result.UserVM.phone
            };

            if (result.DriverVM.driverId != 0)
            {
                model.Driver = new Driver()
                {
                    DriverId = result.DriverVM.driverId,
                    Name = result.DriverVM.name,
                    CarPlateNo = result.DriverVM.car_Plate,
                    Gender = result.DriverVM.gender == null ? 0 : result.DriverVM.gender.Value,
                };
                model.Driver.PhotoURL = ConfigurationManager.AppSettings["UploadPath"] + result.DriverVM.photo_url;
                try
                {
                    model.Driver.PhotoFileName = Path.GetFileName(model.Driver.PhotoURL);
                }
                catch
                {
                    model.Driver.PhotoFileName = "";
                }
            }
            else
            {
                model.Driver = new Driver()
                {
                    DriverId = 0
                };
            }

            model.BookingTrxId = Id;
            model.BookingDateTime = result.BookingVM.booking_datetime;
            model.Status = result.BookingVM.booking_status;
            model.RequestCarType = result.BookingVM.request_Cartype == null ? 0 : result.BookingVM.request_Cartype.Value;
            model.RequestCarTypeName = result.BookingVM.request_Cartype == null ? "-" : ((Cartype)result.BookingVM.request_Cartype).ToString();
            model.From = new Location(){
                Latitude = result.BookingVM.from_lat == null ? 0 : result.BookingVM.from_lat.Value,
                Longitude = result.BookingVM.from_lng == null ? 0 : result.BookingVM.from_lng.Value,
            };
            model.To = new Location(){
                Latitude = result.BookingVM.to_lat == null ? 0 : result.BookingVM.to_lat.Value,
                Longitude = result.BookingVM.to_lng == null ? 0 : result.BookingVM.to_lng.Value,
            };
            model.FromLocation = result.BookingVM.from;
            model.ToLocation = result.BookingVM.to;
            model.EstDistance = result.BookingVM.est_Distance == null ? 0 : result.BookingVM.est_Distance.Value;
            model.EstFares = result.BookingVM.est_Fares == null ? 0 : result.BookingVM.est_Fares.Value;
            model.Start = result.BookingVM.pickup_Datetime;
            model.StatusVM = new StatusVM()
            {
                StatusName = ((BookingStatus)result.BookingVM.booking_status).ToString(),
                StatusColor = ((StatusColor)result.BookingVM.booking_status).ToString()
            };
            model.End = result.BookingVM.journey_End_Datetime;
            model.Remarks = result.BookingVM.remarks;
            model.ManualAssignPending = result.BookingVM.manual_Assign_Flag == true && (BookingStatus)result.BookingVM.booking_status == BookingStatus.Pending ? true : false;

            return View(model);
        }
        
        public ActionResult Assign(int Id)
        {
            BookingTrx model = new BookingTrx();

            var result = BookingService.GetBooking(Id);

            if (result.BookingVM.booking_status == (int)BookingStatus.Assigned)
            {
                TempData["Message"] = CustomErrorType.BookingAlreadyAssign.ToString();
                return RedirectToAction("View", "BookingTrx", new { Id = Id });
            }
            else
            {
                model.User = new User()
                {
                    UserId = result.UserVM.userId,
                    Name = result.UserVM.name,
                    Username = result.UserVM.username,
                    Email = result.UserVM.email,
                    Phone = result.UserVM.phone
                };

                if (result.DriverVM.driverId != 0)
                {
                    model.Driver = new Driver()
                    {
                        DriverId = result.DriverVM.driverId,
                        Name = result.DriverVM.name,
                        CarPlateNo = result.DriverVM.car_Plate,
                        Gender = result.DriverVM.gender == null ? 0 : result.DriverVM.gender.Value,
                    };
                    model.Driver.PhotoURL = result.DriverVM.photo_url;
                    try
                    {
                        model.Driver.PhotoFileName = Path.GetFileName(model.Driver.PhotoURL);
                    }
                    catch
                    {
                        model.Driver.PhotoFileName = "";
                    }
                }
                else
                {
                    model.Driver = new Driver()
                    {
                        DriverId = 0
                    };
                }

                model.BookingTrxId = result.BookingVM.bookingId;
                model.BookingDateTime = result.BookingVM.booking_datetime;
                model.Status = result.BookingVM.booking_status;
                model.RequestCarType = result.BookingVM.request_Cartype == null ? 0 : result.BookingVM.request_Cartype.Value;
                model.RequestCarTypeName = result.BookingVM.request_Cartype == null ? "-" : ((Cartype)result.BookingVM.request_Cartype).ToString();
                model.From = new Location()
                {
                    Latitude = result.BookingVM.from_lat == null ? 0 : result.BookingVM.from_lat.Value,
                    Longitude = result.BookingVM.from_lng == null ? 0 : result.BookingVM.from_lng.Value,
                };
                model.To = new Location()
                {
                    Latitude = result.BookingVM.to_lat == null ? 0 : result.BookingVM.to_lat.Value,
                    Longitude = result.BookingVM.to_lng == null ? 0 : result.BookingVM.to_lng.Value,
                };
                model.FromLocation = result.BookingVM.from;
                model.ToLocation = result.BookingVM.to;
                model.EstDistance = result.BookingVM.est_Distance == null ? 0 : result.BookingVM.est_Distance.Value;
                model.EstFares = result.BookingVM.est_Fares == null ? 0 : result.BookingVM.est_Fares.Value;
                model.Start = result.BookingVM.pickup_Datetime;
                model.StatusVM = new StatusVM()
                {
                    StatusName = ((BookingStatus)result.BookingVM.booking_status).ToString(),
                    StatusColor = ((StatusColor)result.BookingVM.booking_status).ToString()
                };
                model.End = result.BookingVM.journey_End_Datetime;
                model.Remarks = result.BookingVM.remarks;

                return View(model);
            }
        }

        public ActionResult DoAssign(int BookingId, int DriverId)
        {
            try
            {
                HttpCookie Cookie = Request.Cookies["GTCookie"] ?? new HttpCookie("GTCookie");
                int AdminId = int.Parse(Cookie["AdminId"]);

                BookingService.AdminAssignDriver(BookingId, DriverId, AdminId);
                TempData["Message"] = "Booking No. " + BookingId + " successfully assigned to Driver " + DriverService.GetDriver(DriverId).name;
                return RedirectToAction("View", "BookingTrx", new { Id = BookingId });
            }
            catch (Exception e)
            {
                TempData["Message"] = e.Message;
                return RedirectToAction("Assign", "BookingTrx", new { Id = BookingId });
            }
        }

        public ActionResult UnusualActivities()
        {
            return View();
        }

        public ActionResult ListBookingTrxWithUnusualActivity(DataTableParam param)
        {
            try
            {
                var model = new BookingTrxListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 0 ? "BookingId" :
                                            sortColumnIndex == 1 ? "UserName" :
                                            sortColumnIndex == 2 ? "DriverName" :
                                            sortColumnIndex == 3 ? "BookingDateTime" :
                                            sortColumnIndex == 4 ? "From" :
                                            sortColumnIndex == 5 ? "To" :
                                            sortColumnIndex == 6 ? "Status" :
                                            "BookingDateTime";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = BookingService.GetBookingWithUnusualActivities(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection);

                List<BookingTrx> BookingTrxList = new List<BookingTrx>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    BookingTrx VM = new BookingTrx();
                    VM.BookingTrxId = v.BookingVM.bookingId;
                    VM.UserName = v.UserVM.name;
                    VM.DriverName = v.DriverVM.name;
                    VM.BookingDateTimeText = v.BookingVM.booking_datetime == null ? "-" : v.BookingVM.booking_datetime.Value.ToString("dd MMM yyyy hh:mm tt");
                    VM.FromLocation = v.BookingVM.from;
                    VM.ToLocation = v.BookingVM.to;
                    VM.Status = v.BookingVM.booking_status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((BookingStatus)v.BookingVM.booking_status).ToString(),
                        StatusColor = ((StatusColor)v.BookingVM.booking_status).ToString()
                    };

                    BookingTrxList.Add(VM);
                }

                model.aaData = BookingTrxList;
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

	}
}