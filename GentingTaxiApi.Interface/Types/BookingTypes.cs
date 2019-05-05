using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class BookingTypes
    {
    }

    public class BookingVM
    {
        public int bookingId { get; set; }
        public int? userId { get; set; }
        public int? assigned_driverId { get; set; }
        public int? booking_status { get; set; }
        public decimal? from_lat { get; set; }
        public decimal? from_lng { get; set; }
        public decimal? to_lat { get; set; }
        public decimal? to_lng { get; set; }
        public decimal? est_Distance { get; set; }
        public decimal? est_Fares { get; set; }
        public Boolean? manual_Assign_Flag { get; set; }
        public DateTime? pickup_Datetime { get; set; }
        public DateTime? journey_End_Datetime { get; set; }
        public Boolean? usercall_flag { get; set; }
        public Boolean? drivercall_flag { get; set; }
        public Boolean? cancelledby_flag { get; set; }
        public DateTime? booking_datetime { get; set; }
        public DateTime? created_date { get; set; }

        public string driver_name { get; set; }
        public string driver_phoneno { get; set; }
        public string photo_url { get; set; }
        public string car_Plate { get; set; }
        public string ic { get; set; }

        public string user_name { get; set; }
        public string user_phone { get; set; }

        public int? feedbackstatus { get; set; }

        public int? fdid_selected { get; set; }

        //for response purpose 
        public string date { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public decimal? frompx { get; set; }
        public decimal? frompy { get; set; }
        public decimal? topx { get; set; }
        public decimal? topy { get; set; }
        public decimal? edst { get; set; }
        public decimal? eprc { get; set; }

        //for cms purpose
        public int? request_Cartype { get; set; }
        public string remarks { get; set; }
    }

    public class BookingVMResponse
    {

        //for response purpose 
        public string date { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public decimal frompx { get; set; }
        public decimal frompy { get; set; }
        public decimal topx { get; set; }
        public decimal topy { get; set; }
        public decimal edst { get; set; }
        public decimal eprc { get; set; }
        public string status { get; set; }
    }


    public class DriverBookingsVM
    {
        public int driverBookingsId { get; set; }
        public int driverId { get; set; }
        public int bookingId { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public Nullable<System.DateTime> last_updated { get; set; }
        public Nullable<int> status { get; set; }
        public Nullable<System.DateTime> response_datettime { get; set; }
    }

    public class BookingRecentVM
    {
        public string from { get; set; }
        public decimal? frompx { get; set; }
        public decimal? frompy { get; set; }
        public string to { get; set; }
        public decimal? topx { get; set; }
        public decimal? topy { get; set; }
        public DateTime? booking_datetime { get; set; }
        public string date { get; set; }
    }

    //for cms web purpose
    public class BookingDetailVM
    {
        public BookingVM BookingVM { get; set; }
        public UserVM UserVM { get; set; }
        public DriverVM DriverVM { get; set; }
    }

    public class DriverBookingDetailVM
    {
        public DriverBookingsVM DriverBookingsVM { get; set; }
        public BookingVM BookingVM { get; set; }
        public UserVM UserVM { get; set; }
        public DriverVM DriverVM { get; set; }
    }

    public class PNobject
    {
        public string alert { get; set; }
        public int? badge { get; set; }
        public int? bid { get; set; }
        public int? bookingstatus { get; set; }
        public string dest { get; set; }
        public string pickup { get; set; }
        public string bookingtime { get; set; }
        public string name { get; set; }
        public string frompx { get; set; }
        public string frompy { get; set; }
        public string topx { get; set; }
        public string topy { get; set; }
        public int manualassign { get; set; }
        public string to { get; set; }
    }

    public enum BookingStatus
    {
        Cancelled = -1 , 
        Pending = 0 , 
        Assigned = 1 , 
        Pickup = 2 , 
        Completed = 3 , 
        PreferredNoRespond = 4
    }

    public enum Cartype
    {
        Executive = 0 , 
        //Premium = 1 , 
        //extra types 
        TEKS1M = 1 , 
        RW = 2 ,
    }

    public enum DriverBookingStatus
    {
        Pending = 0 ,
        Accepted = 1 , 
        Rejected = 2 , 
        Expired = 3 
    }
}
