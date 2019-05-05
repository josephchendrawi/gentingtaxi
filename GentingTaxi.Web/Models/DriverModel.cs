using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Models
{
    public class Driver
    {
        public int DriverId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Identification Card Number")]
        public string IDCardNo { get; set; }
        [Required]
        public int Gender { get; set; }
        [Display(Name = "Date of Birth")]
        public DateTime? BirthDate { get; set; }
        [Required]
        [Display(Name = "Plate Number")]
        public string CarPlateNo { get; set; }
        [Required]
        [Display(Name = "Car Type")]
        public int CarType { get; set; }
        [Display(Name = "Car Type")]
        public string CarTypeName { get; set; }
        public string PhotoURL { get; set; }
        public string PhotoFileName { get; set; }
        public int? Status { get; set; }
        public StatusVM StatusVM { get; set; }
        [Display(Name = "Registered On")]
        public DateTime? CreatedDate { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [CompareAttribute("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "App Ver.")]
        public string Appversion { get; set; }
        public int Apptype { get; set; }

        [Required]
        public string Phone { get; set; }

        public string LastUpdated { get; set; }

        public Location CurrentLocation { get; set; }
    }
    public class DriverListData : DataTableModel
    {
        public List<Driver> aaData;
    }

    public class DriverAdd : Driver
    {
        [Required]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
    }
    public class DriverEdit : Driver
    {
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class DriverOnOff : Driver
    {
        [Display(Name = "On/Off")]
        public string OnOffStatus { get; set; }
    }
    public class DriverOnOffListData : DataTableModel
    {
        public List<DriverOnOff> aaData;
    }
}