using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class DriverController : BaseController
    {
        DriverService DriverService = new DriverService();

        public ActionResult List()
        {
            return View();
        }
        
        public ActionResult ListDriver(DataTableParam param)
        {
            try
            {
                var model = new DriverListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "IC" :
                                            sortColumnIndex == 3 ? "CarPlate" :
                                            sortColumnIndex == 4 ? "CarType" :
                                            sortColumnIndex == 5 ? "Status" :
                                            "DriverId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = DriverService.GetAllDrivers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Name", param.sSearch == null ? "" : param.sSearch);
                
                List<Driver> DriverList = new List<Driver>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Driver VM = new Driver();
                    VM.DriverId = v.driverId;
                    VM.Name = v.name;
                    VM.IDCardNo = v.ic;
                    VM.CarPlateNo = v.car_Plate == null ? "-" : v.car_Plate;
                    VM.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((DriverStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };

                    DriverList.Add(VM);
                }

                model.aaData = DriverList;
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

        public ActionResult ListSuspendedDriver(DataTableParam param)
        {
            try
            {
                var model = new DriverListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "IC" :
                                            sortColumnIndex == 3 ? "CarPlate" :
                                            sortColumnIndex == 4 ? "CarType" :
                                            sortColumnIndex == 5 ? "Status" :
                                            "DriverId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = DriverService.GetAllSuspendedDrivers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Name", param.sSearch == null ? "" : param.sSearch);

                List<Driver> DriverList = new List<Driver>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Driver VM = new Driver();
                    VM.DriverId = v.driverId;
                    VM.Name = v.name;
                    VM.IDCardNo = v.ic;
                    VM.CarPlateNo = v.car_Plate == null ? "-" : v.car_Plate;
                    VM.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((DriverStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };

                    DriverList.Add(VM);
                }

                model.aaData = DriverList;
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
            DriverAdd model = new DriverAdd();
            model.BirthDate = DateTime.Now;

            return View(model);
        }
        
        [HttpPost]
        public ActionResult Add(DriverAdd Model, HttpPostedFileBase[] files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var vm = new DriverVM();
                    vm.name = Model.Name;
                    vm.ic = Model.IDCardNo;
                    vm.gender = Model.Gender;
                    vm.password = Model.Password;
                    vm.status = Model.Status;
                    vm.car_Plate = Model.CarPlateNo;
                    vm.car_Type = Model.CarType;
                    vm.phone = Model.Phone;
                    vm.dateofbirth = Model.BirthDate;

                    var result = DriverService.AddDriver(vm);

                    if (result != 0)
                    {
                        List<string> filespath = new List<string>();
                        if (files[0] != null && files.Count() > 0)
                        {
                            foreach (var file in files)
                            {
                                filespath.Add(Helper.FileUpload(file));
                            }
                            DriverService.EditDriverPhotoURL(result, filespath.First()); ///
                        }

                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("View", "Driver", new { ID = result });
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
            Driver model = new Driver();

            var v = DriverService.GetDriver(Id);

            model.BirthDate = v.dateofbirth;
            model.DriverId = v.driverId;
            model.Name = v.name;
            model.IDCardNo = v.ic;
            model.CarPlateNo = v.car_Plate == null ? "-" : v.car_Plate;
            model.CarType = v.car_Type == null ? 0 : (int)v.car_Type;
            model.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
            model.Status = v.status;
            model.StatusVM = new StatusVM()
            {
                StatusName = ((DriverStatus)v.status).ToString(),
                StatusColor = ((StatusColor)v.status).ToString()
            };
            model.Gender = v.gender == null ? 0 : (int)v.gender;
            model.Phone = v.phone;
            model.PhotoURL = Helper.GetUploadURL(v.photo_url);
            try
            {
                model.PhotoFileName = Path.GetFileName(model.PhotoURL);
            }
            catch
            {
                model.PhotoFileName = "";
            }
            model.CreatedDate = v.created_date;

            return View(model);
        }

        public ActionResult Edit(int Id)
        {
            DriverEdit model = new DriverEdit();

            var v = DriverService.GetDriver(Id);

            model.BirthDate = v.dateofbirth;
            model.DriverId = v.driverId;
            model.Name = v.name;
            model.IDCardNo = v.ic;
            model.CarPlateNo = v.car_Plate;
            model.CarType = v.car_Type == null ? 0 : (int)v.car_Type;
            model.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
            model.Status = v.status;
            model.StatusVM = new StatusVM()
            {
                StatusName = ((DriverStatus)v.status).ToString(),
                StatusColor = ((StatusColor)v.status).ToString()
            };
            model.Gender = v.gender == null ? 0 : (int)v.gender;
            model.Phone = v.phone;
            model.PhotoURL = Helper.GetUploadURL(v.photo_url);
            try
            {
                model.PhotoFileName = Path.GetFileName(model.PhotoURL);
            }
            catch
            {
                model.PhotoFileName = "";
            }
            model.CreatedDate = v.created_date;
            
            return View(model);
        }
        
        [HttpPost]
        public ActionResult Edit(DriverEdit Model, HttpPostedFileBase[] files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var vm = new DriverVM();
                    vm.driverId = Model.DriverId;
                    vm.name = Model.Name;
                    vm.ic = Model.IDCardNo;
                    vm.gender = Model.Gender;
                    vm.password = Model.Password;
                    vm.status = Model.Status;
                    vm.car_Plate = Model.CarPlateNo;
                    vm.car_Type = Model.CarType;
                    vm.phone = Model.Phone;
                    vm.dateofbirth = Model.BirthDate;

                    var result = DriverService.EditDriver(vm);

                    if (result == true)
                    {
                        List<string> filespath = new List<string>();
                        if (files[0] != null && files.Count() > 0)
                        {
                            foreach (var file in files)
                            {
                                filespath.Add(Helper.FileUpload(file));
                            }
                            DriverService.EditDriverPhotoURL(Model.DriverId, filespath.First()); ///
                        }

                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("View", "Driver", new { ID = Model.DriverId });
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



        public ActionResult ListAvailableDriver(DataTableParam param, int BookingId = 0)
        {
            try
            {
                var model = new DriverListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                          sortColumnIndex == 2 ? "Phone" :
                                          sortColumnIndex == 3 ? "CarPlate" :
                                          sortColumnIndex == 6 ? "Nearest" :
                                          sortColumnIndex == 4 ? "LastUpdated" :
                                          "DriverId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = DriverService.GetAllAvailableDrivers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Name", param.sSearch == null ? "" : param.sSearch, BookingId);

                List<Driver> DriverList = new List<Driver>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Driver VM = new Driver();
                    VM.DriverId = v.driverId;
                    VM.Name = v.name;
                    VM.IDCardNo = v.ic;
                    VM.CarPlateNo = v.car_Plate == null ? "-" : v.car_Plate;
                    VM.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((DriverStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };
                    VM.Phone = v.phone;
                    VM.LastUpdated = v.last_updated == null ? "" : v.last_updated.Value.ToString("dd MMM yyyy HH:mm");

                    DriverList.Add(VM);
                }

                model.aaData = DriverList;
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

        public ActionResult OnOff()
        {
            int TotalCount = 0;
            int OnCount = 0;
            int OffCount = 0;
            DriverService.GetAllDrivers(0, 0, ref TotalCount, ref OnCount, ref OffCount, "", "", "Name", "");

            ViewBag.OnCount = OnCount;
            ViewBag.OffCount = OffCount;
            
            return View();
        }

        public ActionResult ListDriverOnOff(DataTableParam param)
        {
            try
            {
                var model = new DriverOnOffListData();

                int TotalCount = 0;
                int OnCount = 0;
                int OffCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "OnOffStatus" :
                                            sortColumnIndex == 2 ? "Name" :
                                            sortColumnIndex == 3 ? "IC" :
                                            sortColumnIndex == 4 ? "CarPlate" :
                                            sortColumnIndex == 5 ? "CarType" :
                                            "DriverId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = DriverService.GetAllDrivers(param.iDisplayStart, param.iDisplayLength, ref TotalCount, ref OnCount, ref OffCount, orderingFunction, sortDirection, "Name", param.sSearch == null ? "" : param.sSearch);

                List<DriverOnOff> DriverList = new List<DriverOnOff>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    DriverOnOff VM = new DriverOnOff();
                    VM.DriverId = v.driverId;
                    VM.Name = v.name;
                    VM.IDCardNo = v.ic;
                    VM.CarPlateNo = v.car_Plate == null ? "-" : v.car_Plate;
                    VM.CarTypeName = v.car_Type == null ? "-" : ((Cartype)v.car_Type).ToString();
                    VM.Status = v.status;
                    VM.StatusVM = new StatusVM()
                    {
                        StatusName = ((DriverStatus)v.status).ToString(),
                        StatusColor = ((StatusColor)v.status).ToString()
                    };
                    VM.OnOffStatus = v.OnOffStatus;
                    VM.Appversion = v.app_version;
                    VM.Apptype = v.app_type;

                    DriverList.Add(VM);
                }

                model.aaData = DriverList;
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