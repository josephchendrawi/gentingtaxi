using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using GentingTaxiApi.Interface.Types;


namespace GentingTaxiApi.Interface.Operations
{
    public class Users
    {
    }

    [Route("/register", "POST")]
    public class RegisterUserRequest : IReturn<UserResponse>
    {
        public string username { get; set; }
        public string password { get; set; }
        public string confirm_password { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string countrycode { get; set; }
    }

    [Route("/activate", "POST")]
    public class ActivateUserRequest : IReturn<UserResponse>
    {
        public string username { get; set; }
        public string pin { get; set; }
    }

    [Route("/login", "POST")]
    public class LoginUserRequest : IReturn<UserResponse>
    {
        public string username { get; set; }
        public string password { get; set; }
        public string deviceId { get; set; }
        public string app_version { get; set; }
        public int app_type { get; set; }
    }

    [Route("/validateapp", "POST")]
    public class ValidateUserAppRequest : IReturn<ValidateResponse>
    {
    }

    [Route("/updatedeviceid", "POST")]
    public class UpdateUserDeviceIdRequest : IReturn<UserResponse>
    {
        public string deviceId { get; set; }
        public string token { get; set; }
    }

    [Route("/resend", "POST")]
    public class ResendUserCodeRequest : IReturn<UserResponse>
    {
        public string username { get; set; }
        public string phone { get; set; }
        public string countrycode { get; set; }
    }

    [Route("/update", "POST")]
    public class UpdateUserRequest : IReturn<UserResponse>
    {
        public string email { get; set; }
        public string phone { get; set; }
        public string countrycode { get; set; }
        public string name { get; set; }
        public string token { get; set; }
    }

    [Route("/logout", "POST")]
    public class LogoutCurrentUserRequest : IReturn<UserResponse>
    {
        public string token { get; set; }
        public int userid { get; set; }
    }

    [Route("/who", "GET")]
    public class GetCurrentUserRequest : IReturn<UserResponse>
    {
        public string token { get; set; }
    }

    [Route("/pdrivers"  , "GET")]
    public class GetCurrentUserPreferredDriverRequest : IReturn<PreferredDriverResponse>
    {
        public string token { get; set; }
    }

    [Route("/alldrivers", "GET")]
    public class GetAllDriverRequest : IReturn<PreferredDriverResponse>
    {
        public string token { get; set; }
    }



    [Route("/driverlocation/{bookingId}", "GET")]
    public class GetDriverLocationRequest : IReturn<DriverLocationResponse>
    {
        public string token { get; set; }
        public int bookingId { get; set; }
    }

    [Route("/feedback", "POST")]
    public class SubmitFeedbackRequest : IReturn<UserResponse>
    {
        public string token { get; set; }
        public string remarks { get; set; }
        public int bookingid { get; set; }
    }

    [Route("/SetUserPreferredDriver/{driverID}", "POST")]
    public class SetCurrentUserPreferredDriverRequest : IReturn<PreferredDriverResponse>
    {
        public int driverID { get; set; }
        public string token { get; set; }
    }

    [Route("/deletePreferredDriver/{did}", "DELETE")]
    public class DeleteCurrentUserPreferredDriverRequest : IReturn<PreferredDriverResponse>
    {
        public int did { get; set; }
        public string token { get; set; }
    }

    [Route("/bookings/from", "GET")]
    public class GetCurrentUserBookingFromRequest : IReturn<BookingFromListResponse>
    {
        public string token { get; set; }
    }

    [Route("/bookings/to", "GET")]
    public class GetCurrentUserBookingToRequest : IReturn<BookingToListResponse>
    {
        public string token { get; set; }
    }

    [Route("/checktoken", "GET")]
    public class CheckUserTokenRequest : IReturn<BookingToListResponse>
    {
        public string token { get; set; }
    }

    [Route("/getnearbydrivers", "POST")]
    public class GetNearbyDriversRequest : IReturn<NearByDriverResponse>
    {
        public string token { get; set; }
        public decimal lng { get; set; }
        public decimal lat { get; set; }
    }

    public class UserResponse : Response
    {
        public UserVM result { get; set; }
        public string token { get; set; }
        public string pin { get; set; }
    }

    public class NearByDriverResponse : Response
    {
        public List<CoordinateVM> result { get; set; }
    }

    public class PreferredDriverResponse : Response
    {
        public List<DriverVM> result { get; set; }
    }

    public class ValidateResponse : Response
    {
        public bool isCurrentVersion { get; set; }
    }
}