using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Models
{
    public class LoginModel
    {
        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class Admin
    {
        public int AdminId { get; set; }
        [Required]
        public string Name { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [CompareAttribute("Password")]
        public string ConfirmPassword { get; set; }
        [Required]
        [Display(Name = "Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        public string Username { get; set; }
    }
    public class AdminListData : DataTableModel
    {
        public List<Admin> aaData;
    }

    public class AdminAdd : Admin
    {
        [Required]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
    }
    public class AdminEdit : Admin
    {
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }


    public class AdminNotification
    {
        public int AdminNotificationId { get; set; }
        public int AdminId { get; set; }
        public string Message { get; set; }
        public bool isRead { get; set; }

        public int BookingID { get; set; }
    }
    public class AdminNotificationListData : DataTableModel
    {
        public List<AdminNotification> aaData;
    }

    public class AdminSettingView
    {
        [Display(Name = "Wait time for searching next batch of drivers (in seconds)")]
        public long? searchdriverbuffer_sec { get; set; }

        [Display(Name = "Driver availability after booked range (in minutes)")]
        public int? driveravailabilityrange_min { get; set; }

        [Display(Name = "Location range for recommended location (in miles)")]
        public decimal? location_range_miles { get; set; }

        [Display(Name = "Current driver app android version")]
        public string current_driver_android_app_version { get; set; }
        [Display(Name = "Current driver app ios version")]
        public string current_driver_ios_app_version { get; set; }
        [Display(Name = "Current user app android version")]
        public string current_user_android_app_version { get; set; }
        [Display(Name = "Current user app ios version")]
        public string current_user_ios_app_version { get; set; }
    }
}