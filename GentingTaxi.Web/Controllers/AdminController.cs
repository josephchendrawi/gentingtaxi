using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace GentingTaxi.Controllers
{
    public class AdminController : BaseController
    {
        public AdminService AdminService = new AdminService();
        public AdminNotificationService AdminNotificationService = new AdminNotificationService();

        public ActionResult Login(string ReturnURL = "")
        {
            LoginModel model = new LoginModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, FormCollection form)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string Name = "";
                    //Login Process
                    var result = AdminService.LoginAdminService(model.Email, model.Password, ref Name);

                    if (result != 0)
                    {
                        HttpCookie Cookie = Request.Cookies["GTCookie"] ?? new HttpCookie("GTCookie");
                        Cookie["Email"] = model.Email;
                        Cookie["Name"] = Name;
                        Cookie["AdminId"] = result.ToString();
                        Cookie.Expires = DateTime.Now.AddDays(365);
                        Response.Cookies.Add(Cookie);

                        FormsAuthentication.SetAuthCookie(result.ToString(), model.RememberMe);

                        if (form["ReturnURL"] == null || form["ReturnURL"] == "")
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            return Redirect(form["ReturnURL"]);
                        }
                    }
                    else
                        TempData["Message"] = "Invalid Email or Password.";
                }
                catch (Exception ex)
                {
                    TempData["Message"] = ex.Message;
                }
            }

            return View(model);
        }

        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            Response.Cookies.Remove("GTCookie");
            return RedirectToAction("Login", "Admin");
        }

        public ActionResult List()
        {
            return View();
        }

        public ActionResult ListAdmin(DataTableParam param)
        {
            try
            {
                var model = new AdminListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "Email" :
                                            "UserId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = AdminService.GetAllAdmin(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Email", param.sSearch == null ? "" : param.sSearch);

                List<Admin> AdminList = new List<Admin>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Admin VM = new Admin();
                    VM.AdminId = v.adminId;
                    VM.Name = v.name;
                    VM.Username = v.username;

                    AdminList.Add(VM);
                }

                model.aaData = AdminList;
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

        public ActionResult Add()
        {
            AdminAdd model = new AdminAdd();

            return View(model);
        }

        [HttpPost]
        public ActionResult Add(AdminAdd Model, HttpPostedFileBase[] files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var vm = new AdminVM();
                    vm.name = Model.Name;
                    vm.username = Model.Username;
                    vm.password = Model.Password;

                    var result = AdminService.AddAdmin(vm);

                    if (result != 0)
                    {
                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("List", "Admin");
                    }
                }
                catch (Exception ex)
                {
                    TempData["Message"] = ex.Message;
                }
            }
            else
            {
                TempData["Message"] = string.Join(" ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
            }

            return View(Model);
        }

        public ActionResult Edit(int Id)
        {
            AdminEdit model = new AdminEdit();

            var v = AdminService.GetAdmin(Id);

            model.AdminId = v.adminId;
            model.Name = v.name;

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(AdminEdit Model, HttpPostedFileBase[] files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var vm = new AdminVM();
                    vm.adminId = Model.AdminId;
                    vm.name = Model.Name;
                    vm.username = Model.Username;
                    vm.password = Model.Password;

                    var result = AdminService.EditAdmin(vm);

                    if (result == true)
                    {
                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("List", "Admin");
                    }
                }
                catch (Exception ex)
                {
                    TempData["Message"] = ex.Message;
                }
            }
            else
            {
                TempData["Message"] = string.Join(" ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
            }

            return View(Model);
        }

        public ActionResult Notifications()
        {
            return View();
        }

        public ActionResult ListNotification(DataTableParam param)
        {
            try
            {
                HttpCookie Cookie = Request.Cookies["GTCookie"] ?? new HttpCookie("GTCookie");
                int AdminId = int.Parse(Cookie["AdminId"]);

                var model = new AdminNotificationListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = "CreatedDate";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = AdminNotificationService.GetAllAdminNotifications(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, param.sSearch == null ? "" : param.sSearch, AdminId);

                List<AdminNotification> AdminNotificationList = new List<AdminNotification>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    AdminNotification VM = new AdminNotification();
                    VM.AdminId = v.adminId;
                    VM.AdminNotificationId = v.adminNotificationId;
                    VM.isRead = v.isRead;
                    VM.Message = v.message;
                    try
                    {
                        VM.BookingID = int.Parse(v.message.Replace("New Booking Transaction Created with Booking ID : ", ""));
                    }
                    catch
                    {
                        VM.BookingID = 0;
                    }

                    AdminNotificationList.Add(VM);
                }

                model.aaData = AdminNotificationList;
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

        [HttpPost]
        public string ReadNotification()
        {
            try
            {
                HttpCookie Cookie = Request.Cookies["GTCookie"] ?? new HttpCookie("GTCookie");
                int AdminId = int.Parse(Cookie["AdminId"]);

                AdminNotificationService.ReadAllNotifications(AdminId);

                return "success";
            }
            catch
            {
                return "error";
            }
        }

        #region Admin Setting 

        public ActionResult Settings()
        {
            //get default setting 
            var adminsettingobj = AdminService.GetAdminSetting();

            AdminSettingView model = new AdminSettingView();

            if (adminsettingobj != null)
            {
                model.driveravailabilityrange_min = adminsettingobj.driveravailabilityrange_min;
                model.location_range_miles = adminsettingobj.location_range_miles;
                model.searchdriverbuffer_sec = adminsettingobj.searchdriverbuffer_sec;
                model.current_driver_android_app_version = adminsettingobj.current_driver_android_app_version;
                //model.current_driver_ios_app_version = adminsettingobj.current_driver_ios_app_version; 
                model.current_user_android_app_version = adminsettingobj.current_user_android_app_version;
                model.current_user_ios_app_version = adminsettingobj.current_user_ios_app_version;
            }

            return View(model); 
        }

        [HttpPost]
        public ActionResult Settings(AdminSettingView model)
        {
            //update setting 
            try
            {
                var adminsettingentity = new AdminSettingVM();
                adminsettingentity.driveravailabilityrange_min = model.driveravailabilityrange_min ?? default(int);
                adminsettingentity.location_range_miles = model.location_range_miles ?? default(decimal);
                adminsettingentity.searchdriverbuffer_sec = model.searchdriverbuffer_sec ?? default(long);
                adminsettingentity.current_driver_android_app_version = model.current_driver_android_app_version;
                adminsettingentity.current_driver_ios_app_version = model.current_driver_ios_app_version;
                adminsettingentity.current_user_android_app_version = model.current_user_android_app_version;
                adminsettingentity.current_user_ios_app_version = model.current_user_ios_app_version;

                AdminService.UpdateAdminSetting(adminsettingentity);

                TempData["Message"] = "Setting saved";
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
            }

            return RedirectToAction("Settings");
        }

        #endregion


    }
}