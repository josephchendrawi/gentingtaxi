using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GentingTaxi.Models
{
    public class BookingTrx
    {
        [Display(Name = "ID")]
        public int BookingTrxId { get; set; }
        [Display(Name = "Booking Time")]

        public DateTime? BookingDateTime { get; set; }
        [Display(Name = "Created Time")]
        public string CreatedDateText { get; set; }

        public string BookingDateTimeText { get; set; }
        public User User { get; set; }
        [Display(Name = "User")]
        public string UserName { get; set; }
        [Display(Name = "Driver")]
        public Driver Driver { get; set; }
        [Display(Name = "Driver")]
        public string DriverName { get; set; }
        public int? Status { get; set; }
        public StatusVM StatusVM { get; set; }
        [Display(Name = "Request Car Type")]
        public int RequestCarType { get; set; }
        public string RequestCarTypeName { get; set; }
        public Location From { get; set; }
        public Location To { get; set; }
        [Display(Name = "From")]
        public string FromLocation { get; set; }
        [Display(Name = "To")]
        public string ToLocation { get; set; }
        [Display(Name = "Estimate Distance")]
        public decimal EstDistance { get; set; }
        [Display(Name = "Estimate Fares")]
        public decimal EstFares { get; set; }
        [Display(Name = "Pickup Date Time")]
        public DateTime? Start { get; set; }
        [Display(Name = "Journey Ended Date Time")]
        public DateTime? End { get; set; }
        public string Remarks { get; set; }
        public bool? ManualAssignPending { get; set; }
    }
    public class BookingTrxListData : DataTableModel
    {
        public List<BookingTrx> aaData;
    }

    public class DriverBookings
    {
        public int DriverBookingsId { get; set; }
        public Driver Driver { get; set; }
        public User User { get; set; }
        public int BookingId { get; set; }
        public string CreatedDate { get; set; }
        public string LastUpdated { get; set; }
        public int? Status { get; set; }
        public StatusVM StatusVM { get; set; }
        public string ResponseDateTime { get; set; }
    }
    public class DriverBookingsListData : DataTableModel
    {
        public List<DriverBookings> aaData;
    }

}