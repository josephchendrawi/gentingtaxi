using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class AdminTypes
    {
    }

    public class AdminVM
    {
        public int adminId { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public string password { get; set; }
    }

    public class AdminNotificationVM
    {
        public int adminNotificationId { get; set; }
        public string message { get; set; }
        public int adminId { get; set; }
        public bool isRead { get; set; }
    }

    public class AdminSettingVM
    {
        public long? searchdriverbuffer_sec { get; set; }
        public int? driveravailabilityrange_min { get; set; }
        public decimal? location_range_miles { get; set; }
        public string current_driver_ios_app_version { get; set; }
        public string current_driver_android_app_version { get; set; }
        public string current_user_ios_app_version { get; set; }
        public string current_user_android_app_version { get; set; }
    }

}
