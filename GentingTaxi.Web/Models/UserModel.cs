using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Models
{
    public class User
    {
        public int UserId { get; set; }
        [Required]
        public string Name { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [CompareAttribute("Password")]
        public string ConfirmPassword { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? Status { get; set; }
        public StatusVM StatusVM { get; set; }
        [Display(Name = "Registered On")]
        public DateTime? CreatedDate { get; set; }
    }
    public class UserListData : DataTableModel
    {
        public List<User> aaData;
    }

    public class Feedback
    {
        public int FeedbackId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string RelativeTime { get; set; }
        public DateTime CreatedDate { get; set; }

        public int? Previous { get; set; }
        public int? Next { get; set; }
    }
    public class FeedbackListData : DataTableModel
    {
        public List<Feedback> aaData;
    }

}