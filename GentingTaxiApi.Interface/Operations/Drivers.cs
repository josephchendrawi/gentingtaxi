using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using GentingTaxiApi.Interface.Types;

namespace GentingTaxiApi.Interface.Operations
{
    class Drivers
    {
    }

    [Route("/d/register", "POST")]
    public class RegisterDriverRequest : IReturn<DriverResponse>
    {
        public string username { get; set; }
        public string password { get; set; }
        public string confirm_password { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string countrycode { get; set; }
    }

    [Route("/d/activate", "POST")]
    public class ActivateDriverRequest : IReturn<DriverResponse>
    {
        public string username { get; set; }
        public string pin { get; set; }
    }

    [Route("/d/login", "POST")]
    public class LoginDriverRequest : IReturn<DriverResponse>
    {
        public string username { get; set; }
        public string password { get; set; }
        public string deviceId { get; set; }
        public string app_version { get; set; }
        public int app_type { get; set; }
    }

    [Route("/d/updatedeviceid", "POST")]
    public class UpdateDriverDeviceIdRequest : IReturn<DriverResponse>
    {
        public string deviceId { get; set; }
        public string token { get; set; }
    }

    [Route("/d/validateapp", "POST")]
    public class ValidateDriverAppRequest : IReturn<ValidateResponse>
    {
    }

    [Route("/d/update", "POST")]
    public class UpdateDriverRequest : IReturn<DriverResponse>
    {
        public string countrycode { get; set; }
        public string phone { get; set; }
        public string photo_url { get; set; }
        public string car_plate { get; set; }
        public string name { get; set; }
        public DateTime dob { get; set; }
        public int type { get; set; }
        public string token { get; set; }
    }

    [Route("/d/logout", "POST")]
    public class LogoutCurrentDriverRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
        public int driverid { get; set; }
    }

    [Route("/d/who", "GET")]
    public class GetCurrentDriverRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
    }

    [Route("/d/acceptBooking/{bookingId}", "POST")]
    public class AcceptDriverBookingsRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
        public int bookingId { get; set; }
        public int? booking_status { get; set; }
    }

    [Route("/d/updateBookingStatus/{bookingId}", "POST")]
    public class UpdateBookingStatusForCurrentDriverRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
        public int bookingId { get; set; }
        public int? booking_status { get; set; }
    }

    [Route("/d/updateLocation", "POST")]
    public class UpdateDriverLocationRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
        public decimal? current_lat { get; set; }
        public decimal? current_lng { get; set; }
    }

    [Route("/d/sendnotification", "POST")]
    public class SendNotificationRequest : IReturn<Response>
    {
        public string msg { get; set; }
        public string deviceid { get; set; }
        public int bookingId { get; set; }
    }

    [Route("/d/test", "POST")]
    public class testpollingrequest : IReturn<Response>
    {
        public int id { get; set; }
    }

    [Route("/d/checktoken", "GET")]
    public class CheckDriverTokenRequest : IReturn<BookingToListResponse>
    {
        public string token { get; set; }
    }

    [Route("/d/setacebooking", "POST")]
    public class SetDriverAceBookingRequest : IReturn<DriverResponse>
    {
        public string token { get; set; }
        public int driverid { get; set; }
    }


    public class DriverResponse : Response
    {
        public DriverVM result { get;set; }
        public string pin { get; set; }
        public string token { get; set; }
    }

    public class DriverLocationResponse : Response
    {
        public string lat { get; set; }
        public string lng { get; set; }
    }
}
