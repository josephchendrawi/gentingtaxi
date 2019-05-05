using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using GentingTaxiApi.Service.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Controllers
{
    public class RecommendedLocationController : BaseController
    {
        public HotspotService HotspotService = new HotspotService();

        public ActionResult List()
        {
            return View();
        }

        public ActionResult ListHotspot(DataTableParam param)
        {
            try
            {
                var model = new HotspotListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "Name" :
                                            sortColumnIndex == 2 ? "Latitude" :
                                            sortColumnIndex == 3 ? "Longitude" :
                                            "HotspotId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = HotspotService.GetAllHotspots(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Name", param.sSearch == null ? "" : param.sSearch);

                List<Hotspot> HotspotList = new List<Hotspot>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    Hotspot VM = new Hotspot();
                    VM.HotspotId = v.hotspotid;
                    VM.Name = v.hotspotname;
                    VM.Latitude = v.hotspot_lat;
                    VM.Longitude = v.hotspot_lng;

                    HotspotList.Add(VM);
                }

                model.aaData = HotspotList;
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

        public ActionResult ListPricing()
        {
            return View();
        }

        public ActionResult ListHotspotPricing(DataTableParam param)
        {
            try
            {
                var model = new HotspotPricingListData();

                int TotalCount = 0;

                //sorting properties : need to pass respective column to sort in query 
                var sortColumnIndex = Convert.ToInt32(Request["iSortCol_0"]);
                string orderingFunction = sortColumnIndex == 1 ? "From" :
                                            sortColumnIndex == 2 ? "To" :
                                            sortColumnIndex == 3 ? "Price" :
                                            "HotspotPricingId";

                var sortDirection = Request["sSortDir_0"]; // asc or desc

                var List = HotspotService.GetAllHotspotPricings(param.iDisplayStart, param.iDisplayLength, ref TotalCount, orderingFunction, sortDirection, "Price", param.sSearch == null ? "" : param.sSearch);

                List<HotspotPricing> HotspotPricingList = new List<HotspotPricing>();

                long count = param.iDisplayStart + 1;
                foreach (var v in List)
                {
                    HotspotPricing VM = new HotspotPricing();
                    VM.HotspotPricingId = v.hotspotpricingid;
                    VM.FromHotspotId = v.from_hotspot ?? 0;
                    VM.ToHotspotId = v.to_hotspot ?? 0;
                    VM.Price = v.price ?? 0;

                    try { VM.FromHotspotName = HotspotService.GetHotspot(VM.FromHotspotId).hotspotname; }
                    catch { VM.FromHotspotName = ""; }

                    try { VM.ToHotspotName = HotspotService.GetHotspot(VM.ToHotspotId).hotspotname; }
                    catch { VM.ToHotspotName = ""; }

                    HotspotPricingList.Add(VM);
                }

                model.aaData = HotspotPricingList;
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
            Hotspot model = new Hotspot();

            return View(model);
        }

        [HttpPost]
        public ActionResult Add(Hotspot Model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var vm = new HotspotVM();
                    vm.hotspotname = Model.Name;
                    vm.hotspot_lat = Model.Latitude;
                    vm.hotspot_lng = Model.Longitude;

                    var result = HotspotService.AddHotspot(vm);

                    if (result != 0)
                    {
                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("List", "RecommendedLocation");
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

        public ActionResult AddPricing()
        {
            HotspotPricing model = new HotspotPricing();

            var HotspotList = new List<Hotspot>();
            int TotalCount = 0;
            var List = HotspotService.GetAllHotspots(0, int.MaxValue, ref TotalCount);
            foreach (var v in List)
            {
                HotspotList.Add(new Hotspot()
                {
                    HotspotId = v.hotspotid,
                    Name = v.hotspotname
                });
            }
            ViewBag.HotspotList = HotspotList;

            return View(model);
        }

        [HttpPost]
        public ActionResult AddPricing(HotspotPricing Model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Model.FromHotspotId == Model.ToHotspotId)
                    {
                        throw new Exception("Both From and To Location cannot be same.");
                    }
                    else if (Model.FromHotspotId == 0 || Model.ToHotspotId == 0)
                    {
                        throw new Exception("From or To Location is empty.");
                    }
                    else if (Model.Price <= 0)
                    {
                        throw new Exception("Price has to be more than 0.");
                    }

                    var vm = new HotspotPricingVM();
                    vm.from_hotspot = Model.FromHotspotId;
                    vm.to_hotspot = Model.ToHotspotId;
                    vm.price = Model.Price;

                    var result = HotspotService.AddHotspotPricing(vm);

                    if (result != 0)
                    {
                        TempData["Message"] = "Successfully done.";
                        return RedirectToAction("ListPricing", "RecommendedLocation");
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

            var HotspotList = new List<Hotspot>();
            int TotalCount = 0;
            var List = HotspotService.GetAllHotspots(0, int.MaxValue, ref TotalCount);
            foreach (var v in List)
            {
                HotspotList.Add(new Hotspot()
                {
                    HotspotId = v.hotspotid,
                    Name = v.hotspotname
                });
            }
            ViewBag.HotspotList = HotspotList;

            return View(Model);
        }


    }
}