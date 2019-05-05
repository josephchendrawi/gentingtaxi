using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GentingTaxi.Models
{
    public class StatusVM
    {
        public string StatusName { get; set; }
        public string StatusColor { get; set; }
    }

    public class OverallStatus
    {
        public int BookingCount { get; set; }
        public int DriverCount { get; set; }
        public int UserCount { get; set; }
    }

    public class Location
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

}