using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using ServiceStack.Caching;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace GentingTaxiApi.Service.API
{
    public class DriverApiService : ServiceStack.Service
    {
        DriverService driverserv = new DriverService();
        BookingService bookserv = new BookingService();

        public DriverResponse Post(RegisterDriverRequest request)
        {
            try
            {
                var response = driverserv.RegisterDriverService(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public DriverResponse Post(ActivateDriverRequest request)
        {
            try
            {
                var response = driverserv.ActivateDriverService(request);

                return response;

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public DriverResponse Post(SetDriverAceBookingRequest request)
        {
            try
            {
                var response = driverserv.SetDriverAceBookingService(request);

                return response;

            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public DriverResponse Post(LoginDriverRequest request)
        {
            try
            {
                var response = driverserv.LoginDriverService(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public ValidateResponse Post(ValidateDriverAppRequest request)
        {
            try
            {
                var api_session = base.GetSession();

                int temp;
                int driverid = int.TryParse(api_session.UserAuthId, out temp) ? temp : 0;

                var response = driverserv.ValidateDriverApp(driverid);

                return new ValidateResponse() { isCurrentVersion = response, sts = 0 };
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new ValidateResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public DriverResponse Post(UpdateDriverDeviceIdRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.UpdateDriverDeviceIdService(request , api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public DriverResponse Post(UpdateDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.UpdateCurrentDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public DriverResponse Get(GetCurrentDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.GetCurrentDriverService(request, api_session);
                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public DriverResponse Post(LogoutCurrentDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.LogoutDriverService(request, api_session);

                //clear session 
                if (api_session != null)
                {
                    using (var cache = TryResolve<ICacheClient>())
                    {
                        cache.Remove(api_session.Id);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public DriverResponse POST(UpdateDriverLocationRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.UpdateDriverLocationService(request, api_session);


                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public DriverResponse GET(CheckDriverTokenRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = driverserv.CheckDriverTokenService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public Response POST(SendNotificationRequest request)
        {
            try
            {
                var response = driverserv.SendNotificationToDriver(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new DriverResponse() { sts = 1, msg = ex.Message };
            }
        }

        public Response POST(testpollingrequest request)
        {
            testfunc(request.id);

            return new Response() { sts = 0}; 
        }

        private void testfunc(int id)
        {
            int sequenceCount = 1;
            int sequenceEnd = 3;

            Timer atimer = new Timer(10000);
            atimer.Elapsed += (o, s) =>
            {
                if (sequenceCount > sequenceEnd)
                {
                    atimer.Stop();
                }

                using (var context = new entity.gtaxidbEntities())
                {
                    entity.Journey abc = new entity.Journey() { created_date = DateTime.Now , bookingId = id };
                    context.Journeys.Add(abc);
                    context.SaveChanges();
                }

                sequenceCount++;
            };

            atimer.Start();
        }
    }
}
