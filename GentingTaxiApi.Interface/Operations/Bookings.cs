using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using GentingTaxiApi.Interface.Types;

namespace GentingTaxiApi.Interface.Operations
{
    class Bookings
    {
    }

    [Route("/bookings", "POST")]
    public class AddBookingRequest : IReturn<BookingResponse>
    {
        public string frmpx { get; set; } //from lat 
        public string frmpy { get; set; } //frm lng 
        public string topx { get; set; } //to lat 
        public string topy { get; set; } //to lng 
        public string from { get; set; } //from loc name 
        public string to { get; set; } //to loc name
        public string date { get; set; } //created date
        public int? fdid { get; set; } //favourite driver 
        public string rmk { get; set; } //remarks
        public decimal edst { get; set; } //est. distance 
        public decimal eprc { get; set; } //est. price 
        public int? type { get; set; } //car type preference
        public string voucher { get; set; }

        public string token { get; set; }
    }

    [Route("/resend_bookings", "POST")]
    public class ResendBookingRequest : IReturn<BookingResponse>
    {
        public int booking_id { get; set; }
        public int fdid { get; set; }

        public string token { get; set; }
    }

    [Route("/Estimatefare", "POST")]
    public class EstimateFareRequest : IReturn<EstimateFareResponse>
    {
        public double distance { get; set; }
        public int UserId { get; set; }
        public int type { get; set; }
        public double frompx { get; set; }
        public double frompy { get; set; }
        public double topx { get; set; }
        public double topy { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string email { get; set; }
    }

    [Route("/ExpandSearch/{BookingId}", "POST")]
    public class ExpandDriverSearchrequest : IReturn<BookingResponse>
    {
        public int BookingId { get; set; }
        public string token { get; set; }
    }

    [Route("/bookings/{BookingId}", "GET")]
    public class GetBookingbyIDrequest : IReturn<BookingResponse>
    {
        public int BookingId { get; set; }
        public string token { get; set; }
    }

    [Route("/d/bookings/{BookingId}", "GET")]
    public class GetBookingbyIDForDriverRequest : IReturn<BookingResponse>
    {
        public int BookingId { get; set; }
        public string token { get; set; }
    }

    [Route("/bookings", "GET")]
    public class GetBookingByCurrentUserRequest : IReturn<BookingListResponse>
    {
        public string token { get; set; }
    }

    [Route("/d/bookings", "GET")]
    public class GetBookingByCurrentDriverRequest : IReturn<BookingListResponse>
    {
        public string token { get; set; }
    }

    [Route("/cancelBooking/{bookingId}", "POST")]
    public class CancelBookingByCurrentUserRequest : IReturn<BookingResponse>
    {
        public string token { get; set; }
        public int bookingId { get; set; }
    }

    [Route("/journey/", "POST")]
    public class AddJourneyEntryRequest : IReturn<BookingResponse>
    {
        public int bookingId { get; set; }
        public DateTime? current_datetime { get; set; }
        public decimal? lat { get; set; }
        public decimal? lng { get; set; }
    }

    [Route("/reassign/{bookingId}", "POST")]
    public class ReassignBookingRequest : IReturn<BookingResponse>
    {
        public int bookingId { get; set; }
    }

    [Route("/web/push-notification")]
    public class PushNotificationToWeb
    {
        public string Message { get; set; }
    }

    public class BookingResponse : Response
    {
        public BookingVM result { get; set; }
        public int booking_id { get; set; }
        public int fdid { get; set; }
    }

    public class BookingListResponse : Response
    {
        public List<BookingVM> result { get; set; }
    }


    public class BookingFromListResponse : Response
    {
        public List<BookingRecentVM> result { get; set; }
    }
    public class BookingToListResponse : Response
    {
        public List<BookingRecentVM> result { get; set; }
    }
    
    public class EstimateFareResponse : Response 
    {
        public double EstimatedFare {get;set;}
        public double Discount { get; set; }
    }
}
