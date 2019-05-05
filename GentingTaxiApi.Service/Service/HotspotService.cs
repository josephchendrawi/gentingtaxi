using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Service.Service
{
    public class HotspotService
    {
        public HotspotVM GetHotspot(int HotspotId)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Hotspots
                          where d.hotspotid == HotspotId
                          select d;

                if (ett.Count() > 0)
                {
                    var v = ett.First();
                    HotspotVM vm = new HotspotVM();
                    vm.hotspotid = v.hotspotid;
                    vm.hotspotname = v.hotspotname;
                    vm.hotspot_lat = v.hotspot_lat;
                    vm.hotspot_lng = v.hotspot_lng;
                    vm.status = v.status;

                    return vm;
                }
                else
                {
                    throw new CustomException(CustomErrorType.HotspotNotFound);
                }
            }
        }

        public List<HotspotVM> GetAllHotspots(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<HotspotVM> result = new List<HotspotVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Hotspots
                          where d.status == (int)HotspotStatus.Active
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "Name")
                        ett = ett.Where(m => m.hotspotname.ToLower().Contains(filterQuery.ToLower()));
                    else if (filterBy == "Latitude")
                        ett = ett.Where(m => m.hotspot_lat == decimal.Parse(filterQuery));
                    else if (filterBy == "Longitude")
                        ett = ett.Where(m => m.hotspot_lng == decimal.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "Name")
                        ett = ett.OrderBy(m => m.hotspotname);
                    else if (orderBy == "Latitude")
                        ett = ett.OrderBy(m => m.hotspot_lat);
                    else if (orderBy == "Longitude")
                        ett = ett.OrderBy(m => m.hotspot_lng);
                    else
                        ett = ett.OrderBy(m => m.hotspotid);
                }
                else
                {
                    if (orderBy == "Name")
                        ett = ett.OrderByDescending(m => m.hotspotname);
                    else if (orderBy == "Latitude")
                        ett = ett.OrderByDescending(m => m.hotspot_lat);
                    else if (orderBy == "Longitude")
                        ett = ett.OrderByDescending(m => m.hotspot_lng);
                    else
                        ett = ett.OrderByDescending(m => m.hotspotid);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    HotspotVM vm = new HotspotVM();
                    vm.hotspotid = v.hotspotid;
                    vm.hotspotname = v.hotspotname;
                    vm.hotspot_lat = v.hotspot_lat;
                    vm.hotspot_lng = v.hotspot_lng;
                    vm.status = v.status;
                    result.Add(vm);
                }
            }

            return result;
        }

        public int AddHotspot(HotspotVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Hotspots
                          where d.hotspotname == vm.hotspotname
                          select d;

                if (ett.Count() > 0)
                {
                    throw new CustomException(CustomErrorType.HotspotAlreadyAssign);
                }
                else
                {
                    Hotspot v = new Hotspot();
                    v.hotspotname = vm.hotspotname;
                    v.hotspot_lat = vm.hotspot_lat;
                    v.hotspot_lng = vm.hotspot_lng;
                    v.created_date = DateTime.Now;
                    v.status = (int)HotspotStatus.Active;

                    context.Hotspots.Add(v);
                    context.SaveChanges();

                    return v.hotspotid;
                }
            }
        }

        public List<HotspotPricingVM> GetAllHotspotPricings(int startIdx, int length, ref int TotalCount, string orderBy = "", string orderDirection = "", string filterBy = "", string filterQuery = "")
        {
            List<HotspotPricingVM> result = new List<HotspotPricingVM>();
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Hotspotpricings
                          select d;

                //filtering
                if (filterBy != "" && filterQuery != "")
                {
                    if (filterBy == "From")
                        ett = ett.Where(m => m.from_hotspot == int.Parse(filterQuery));
                    else if (filterBy == "To")
                        ett = ett.Where(m => m.to_hotspot == int.Parse(filterQuery));
                    else if (filterBy == "Price")
                        ett = ett.Where(m => m.price == decimal.Parse(filterQuery));
                }

                TotalCount = ett.Count();

                //ordering && paging
                if (orderDirection == "asc")
                {
                    if (orderBy == "From")
                        ett = ett.OrderBy(m => m.from_hotspot);
                    else if (orderBy == "To")
                        ett = ett.OrderBy(m => m.to_hotspot);
                    else if (orderBy == "Price")
                        ett = ett.OrderBy(m => m.price);
                    else
                        ett = ett.OrderBy(m => m.hotspotpricingid);
                }
                else
                {
                    if (orderBy == "From")
                        ett = ett.OrderByDescending(m => m.from_hotspot);
                    else if (orderBy == "To")
                        ett = ett.OrderByDescending(m => m.to_hotspot);
                    else if (orderBy == "Price")
                        ett = ett.OrderByDescending(m => m.price);
                    else
                        ett = ett.OrderByDescending(m => m.hotspotpricingid);
                }

                ett = ett.Skip(startIdx).Take(length);

                //mapping
                foreach (var v in ett)
                {
                    HotspotPricingVM vm = new HotspotPricingVM();
                    vm.hotspotpricingid = v.hotspotpricingid;
                    vm.from_hotspot = v.from_hotspot;
                    vm.to_hotspot = v.to_hotspot;
                    vm.price = v.price;
                    result.Add(vm);
                }
            }

            return result;
        }

        public int AddHotspotPricing(HotspotPricingVM vm)
        {
            using (var context = new entity.gtaxidbEntities())
            {
                var ett = from d in context.Hotspotpricings
                          where d.from_hotspot == vm.from_hotspot && d.to_hotspot == vm.to_hotspot
                          select d;

                if (ett.Count() > 0)
                {
                    throw new CustomException(CustomErrorType.HotspotPricingAlreadyAssign);
                }
                else
                {
                    Hotspotpricing v = new Hotspotpricing();
                    v.from_hotspot = vm.from_hotspot;
                    v.to_hotspot = vm.to_hotspot;
                    v.price = vm.price;
                    v.created_date = DateTime.Now;

                    context.Hotspotpricings.Add(v);
                    context.SaveChanges();

                    return v.hotspotpricingid;
                }
            }
        }


    }
}
