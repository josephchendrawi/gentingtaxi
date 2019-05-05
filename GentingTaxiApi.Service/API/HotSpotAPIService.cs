using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using ServiceStack.Caching;
using ServiceStack;
using System.Net;

namespace GentingTaxiApi.Service
{
    class HotSpotAPIService : ServiceStack.Service
    {
        //HotSpotService hotspotservice = new HotSpotService();

        [CustomUserAuthenticateFilter]
        public HotSpotResponse Get(GetAllHotspotRequest request)
        {
            try
            {
                var session = base.GetSession();

                using (var context = new entity.gtaxidbEntities())
                {
                    var usertokenobj = Helper.AuthToken(request.token, session);

                    var entityUserToken = from d in context.User_token
                                          where d.token == usertokenobj.key
                                          select d;

                    if (entityUserToken.Count() > 0)
                    {
                        //get hotspot list 
                        var entityhotspots = context.Database.SqlQuery<HotspotVM>(
                                                "Select * from Hotspot "
                                             );

                        if (entityhotspots.Count() > 0)
                        {
                            return new HotSpotResponse() { result = entityhotspots.ToList() , sts=0};
                        }
                        else
                        {
                            return new HotSpotResponse() { sts = 0 };
                        }

                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.UserNotFound);
                    }
                }

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new HotSpotResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public MatchHotspotResponse Post(MatchHotspotRequest request)
        {
            try
            {
                var session = base.GetSession();

                //get range , default 0.5
                AdminService adminservice = new AdminService();
                double locationrange = 
                    adminservice.GetAdminSetting().location_range_miles > 0 ? 
                    (double)adminservice.GetAdminSetting().location_range_miles : 0.5;

                using (var context = new entity.gtaxidbEntities())
                {
                    var usertokenobj = Helper.AuthToken(request.token, session);

                    var entityUserToken = from d in context.User_token
                                          where d.token == usertokenobj.key
                                          select d;

                    if (entityUserToken.Count() > 0)
                    {
                        decimal price = CalculateHotspotPricing(
                            request.from_name , request.to_name , request.frompx, request.frompy, request.topx, request.topy);

                        return new MatchHotspotResponse(){price = price};
                    }
                    else
                    {
                        throw new CustomException(CustomErrorType.UserNotFound);
                    }
                }

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new MatchHotspotResponse() { sts = 1, msg = ex.Message };
            }
        }

        public static decimal CalculateHotspotPricing(string from , string to , string frompx , string frompy , string topx ,string topy )
        {
            try
            {
                //get range , default 0.5
                AdminService adminservice = new AdminService();
                double locationrange = 
                    adminservice.GetAdminSetting().location_range_miles > 0 ? 
                    (double)adminservice.GetAdminSetting().location_range_miles : 0.5;

                decimal price = 0;

                using (var context = new entity.gtaxidbEntities())
                {
                    //search hotspot by name ( 1st priority)
                        var entityhotspotspricing = context.Database.SqlQuery<HotspotPricingVM>(
                                                "select * from Hotspotpricing t0 " + 
                                                "where from_hotspot = (select hotspotid from Hotspot where hotspotname = '"+from+"') " +
                                                "and to_hotspot = (select hotspotid from Hotspot where hotspotname = '" + to+ "')"
                                             );

                        //compare with hotspot list 
                        if (entityhotspotspricing.Count() > 0)
                        {
                            price = entityhotspotspricing.First().price ?? default(decimal);
                        }
                        else
                        {
                            //search hotspot by radius , ( 2nd priority)
                            decimal temp;
                            decimal frompx_dec = decimal.TryParse(frompx, out temp) ? temp : 0;
                            decimal frompy_dec = decimal.TryParse(frompy, out temp) ? temp : 0;
                            decimal topx_dec = decimal.TryParse(topx, out temp) ? temp : 0;
                            decimal topy_dec = decimal.TryParse(topy, out temp) ? temp : 0;

                            var entityHotspotPricingByRadius =
                                context.Database.SqlQuery<HotspotPricingVM>(
                                                "select * from Hotspotpricing t0 " +
                                                "where from_hotspot = " +
                                                "( " +
                                                "select top 1 hotspotid from " +
                                                "( " +
                                                "select hotspotid , hotspotname ,  " +
                                                "(acos(sin(hotspot_lat * 0.0175) * sin({0} * 0.0175) " +
                                                "       + cos(hotspot_lat * 0.0175) * cos({0} * 0.0175) * " +
                                                "         cos(({1} * 0.0175) - (hotspot_lng * 0.0175)) " +
                                                "      ) * 3959 ) AS distance " +
                                                " from Hotspot " +
                                                " ) as frmtable " +
                                                " where distance <= {4} " +
                                                " order by distance asc " +
                                                ") " +
                                                "and to_hotspot = " +
                                                "( " +
                                                "select top 1 hotspotid from " +
                                                "( " +
                                                "select hotspotid , hotspotname , " +
                                                "(acos(sin(hotspot_lat * 0.0175) * sin({2} * 0.0175) " +
                                                "       + cos(hotspot_lat * 0.0175) * cos({2} * 0.0175) * " +
                                                "         cos(({3} * 0.0175) - (hotspot_lng * 0.0175)) " +
                                                "      ) * 3959 ) AS distance " +
                                                " from Hotspot " +
                                                " ) as frmtable " +
                                                " where distance <= {4} " +
                                                " order by distance asc " +
                                                ") ",
                                                frompx_dec, frompy_dec, topx_dec, topy_dec, locationrange
                                             );

                            if (entityHotspotPricingByRadius.Count() > 0)
                            {
                                price = entityHotspotPricingByRadius.First().price ?? default(decimal);
                            }
                        }

                        return price;
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message); 
            }
        }
    }
}
