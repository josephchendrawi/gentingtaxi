using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class UserController : BaseController
    {
        UserService UserService = new UserService();

        public ActionResult List()
        {
            return View();
        }

        public ActionResult ListUser(DataTableParam param)
        {
            try
            {
                var model = new UserListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "Username" :
                                            sortColumnIndex == 3 ? "Email" :
                                            sortColumnIndex == 4 ? "Status" :
                                            "UserId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = UserService.GetAllUsers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Username", param.sSearch == null ? "" : param.sSearch);
                
                List<User> UserList = new List<User>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    User VM = new User();
                    VM.UserId = v.userId;
                    VM.Name = v.name;
                    VM.Username = v.username;
                    VM.Email = v.email;
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((UserStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };

                    UserList.Add(VM);
                }

                model.aaData = UserList;
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
        
        public ActionResult ListSuspended()
        {
            return View();
        }

        public ActionResult ListSuspendedUser(DataTableParam param)
        {
            try
            {
                var model = new UserListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "Username" :
                                            sortColumnIndex == 3 ? "Email" :
                                            sortColumnIndex == 4 ? "Status" :
                                            "UserId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = UserService.GetAllSuspendedUsers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Username", param.sSearch == null ? "" : param.sSearch);

                List<User> UserList = new List<User>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    User VM = new User();
                    VM.UserId = v.userId;
                    VM.Name = v.name;
                    VM.Username = v.username;
                    VM.Email = v.email;
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((UserStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };

                    UserList.Add(VM);
                }

                model.aaData = UserList;
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

        public ActionResult Edit(int Id)
        {
            User model = new User();

            var result = UserService.GetUser(Id);

            model.UserId = Id;
            model.Username = result.username;
            model.Email = result.email;
            model.Name = result.name;
            model.Phone = result.phone;
            model.Status = result.status;
            model.StatusVM = new StatusVM()
            {
                StatusName = ((UserStatus)result.status).ToString(),
                StatusColor = ((StatusColor)result.status).ToString()
            };
            model.CreatedDate = result.created_date;

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(User Model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    UserVM user = new UserVM();
                    user.userId = Model.UserId;
                    user.name = Model.Name;
                    user.email = Model.Email;
                    user.phone = Model.Phone;
                    user.username = Model.Username;
                    user.password = Model.Password;
                    user.status = Model.Status;

                    var result = UserService.EditUser(user);

                    if (result == true)
                    {
                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("View", "User", new { ID = Model.UserId });
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

        public ActionResult View(int Id)
        {
            User model = new User();

            var result = UserService.GetUser(Id);

            model.UserId = Id;
            model.Username = result.username;
            model.Email = result.email;
            model.Name = result.name;
            model.Phone = result.phone;
            model.Status = result.status;
            model.StatusVM = new StatusVM()
            {
                StatusName = ((UserStatus)result.status).ToString(),
                StatusColor = ((StatusColor)result.status).ToString()
            };
            model.CreatedDate = result.created_date;

            return View(model);
        }

        public ActionResult Feedback()
        {
            return View();
        }
        
        public ActionResult ListUserFeedback(DataTableParam param)
        {
            try
            {
                var model = new FeedbackListData();

                int TotalCount = 0;

                var List = UserService.GetAllUserFeedbacks(param.iDisplayStart, param.iDisplayLength, ref TotalCount);

                //sorting here
                //

                List<Feedback> FeedbackList = new List<Feedback>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Feedback VM = new Feedback();
                    VM.FeedbackId = v.feedbackId;
                    VM.Name = v.User.name;
                    VM.Email = v.User.email;
                    VM.Content = v.remarks.Length > 100 ? v.remarks.Substring(0, 100) + "..." : v.remarks;
                    VM.RelativeTime = Helper.TimeAgo(DateTime.Now.AddMinutes(-50)); ////

                    FeedbackList.Add(VM);
                }

                model.aaData = FeedbackList;
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
        
        public ActionResult FeedbackDetails(int Id)
        {
            var result = UserService.GetUserFeedback(Id);

            Feedback VM = new Feedback();
            VM.FeedbackId = result.feedbackId;
            VM.Email = result.User.email;
            VM.Name = result.User.name;
            VM.Content = result.remarks;
            VM.RelativeTime = Helper.TimeAgo(DateTime.Now.AddMinutes(-50)); ////
            VM.CreatedDate = DateTime.Now.AddMinutes(-50); ////

            VM.Previous = result.Previous;
            VM.Next = result.Next;

            return View(VM);
        }

	}
}