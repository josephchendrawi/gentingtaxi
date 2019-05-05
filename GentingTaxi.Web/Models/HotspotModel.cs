using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi.Models
{
    public class Hotspot
    {
        public int HotspotId { get; set; }
        [Required]
        public string Name { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    public class HotspotListData : DataTableModel
    {
        public List<Hotspot> aaData;
    }

    public class HotspotPricing
    {
        public int HotspotPricingId { get; set; }
        [Required]
        [Display(Name = "From")]
        public int FromHotspotId { get; set; }
        public string FromHotspotName { get; set; }
        [Required]
        [Display(Name = "To")]
        public int ToHotspotId { get; set; }
        public string ToHotspotName { get; set; }
        [Required]
        public decimal Price { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    public class HotspotPricingListData : DataTableModel
    {
        public List<HotspotPricing> aaData;
    }
}