using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GentingTaxiApi.Interface.Operations;
using GentingTaxiApi.Interface.Types;
using System.Net;

namespace GentingTaxiApi.Service.API
{
    public class BookingApiService : ServiceStack.Service
    {
        BookingService bookserv = new BookingService();
        
        [CustomUserAuthenticateFilter]
        public BookingResponse Post(AddBookingRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.AddBookingService(request , api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public BookingResponse Post(ResendBookingRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.ResendBookingService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public EstimateFareResponse Post(EstimateFareRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.EstimatePrice(request.distance, request.UserId, request.frompx, request.frompy, request.topx, request.topy, request.from, request.to, request.email, request.type);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new EstimateFareResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public BookingResponse Post(ExpandDriverSearchrequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.ExpandDriverSearchservice(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public BookingResponse Get(GetBookingbyIDrequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.GetBookingByID(request , api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }


        [CustomDriverAuthenticateFilter]
        public BookingResponse Get(GetBookingbyIDForDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.GetBookingByIDForDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }


        [CustomUserAuthenticateFilter]
        public BookingListResponse Get(GetBookingByCurrentUserRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.GetBookingsforCurrentUser(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingListResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public BookingListResponse Get(GetBookingByCurrentDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.GetBookingsforCurrentDriver(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingListResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomUserAuthenticateFilter]
        public BookingResponse Post(CancelBookingByCurrentUserRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.CancelBookingforCurrentUser(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public BookingResponse Post(AcceptDriverBookingsRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.AcceptBookingForCurrentDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        [CustomDriverAuthenticateFilter]
        public BookingResponse Post(UpdateBookingStatusForCurrentDriverRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.UpdateBookingStatusForCurrentDriverService(request, api_session);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        public Response Post(AddJourneyEntryRequest request)
        {
            try
            {
                var api_session = base.GetSession();
                var response = bookserv.AddJourneyEntryService(request);

                return response;
            }
            catch (Exception ex)
            {
                base.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new BookingResponse() { sts = 1, msg = ex.Message };
            }
        }

        public object Any(PushNotificationToWeb request)
        {
            Helper.PushNotificationToWeb(request.Message);

            return "";
        }

    }
}
