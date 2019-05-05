using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class DriverTypes
    {
    }

    public class DriverVM
    {
        public int driverId { get; set; }
        public string name { get; set; }
        public string ic { get; set; }
        public int? gender { get; set; }
        public string car_Plate { get; set; }
        public string photo_url { get; set; }
        public int? status { get; set; }
        public int? car_Type { get; set; }
        public int? priority { get; set; }
        public decimal? priority_percentage { get; set; }
        public decimal? current_lat { get; set; }
        public decimal? current_lng { get; set; }

        public double distance { get; set; }
        public int currentDriverBookingDriverId { get; set; }
        public DateTime? created_date { get; set; }
        public string password { get; set; }

        public DateTime? dateofbirth { get; set; }
        public string phone { get; set; }

        public DateTime? last_updated { get; set; }

        public string app_version { get; set; }
        public int app_type { get; set; }
    }
    public class DriverVMwithOnOffStatus : DriverVM
    {
        public string OnOffStatus { get; set; }
    }
    public class DriverTokenVM
    {
        public string key { get; set; }
        public int driverId { get; set; }
    }

    public class CoordinateVM
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }

    public enum DriverStatus
    {
        Inactive = 0,
        Active = 1,
        Suspended = 2
    }

    public enum Gender
    {
        Male = 1 , 
        Female = 2 
    }

}
