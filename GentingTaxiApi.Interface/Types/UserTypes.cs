using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class UserTypes
    {
    }

    public class UserVM
    {
        public int userId { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string name { get; set; }
        public int? status { get; set; }
        public int? deviceId { get; set; }
        public string activation_code { get; set; }
        public DateTime? created_date { get; set; }
        public string password { get; set; }
        public int isRWuser { get; set; }
    }

    public class UserTokenVM
    {
        public string key { get; set; }
        public int userId { get; set; }
    }

    public class FeedbackVM
    {
        public int feedbackId { get; set; }
        public UserVM User { get; set; }
        public int? status { get; set; }
        public string remarks { get; set; }

        public int? Previous { get; set; }
        public int? Next { get; set; }
    }

    public enum UserStatus
    {
        Inactive = 0 ,
        Active = 1,
        Suspended = 2
    }


    public enum FeedbackStatus
    {
        Inactive = 0,
        Active = 1
    }

    public enum UserType
    {
        user = 1 , 
        driver = 0
    }

    public enum App_Type
    {
        android = 1 , 
        ios = 2 
    }
}
