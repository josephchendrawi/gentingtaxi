using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class HotspotTypes
    {
    }

    public class HotspotVM
    {
        public int hotspotid { get; set; }
        public string hotspotname { get; set; }
        public decimal? hotspot_lat { get; set; }
        public decimal? hotspot_lng { get; set; }
        public int? status { get; set; }
    }

    public class HotspotPricingVM
    {
        public int hotspotpricingid { get; set; }
        public int? from_hotspot { get; set; }
        public int? to_hotspot { get; set; }
        public decimal? price { get; set; }
    }
    public enum HotspotStatus
    {
        Inactive = 0,
        Active = 1
    }

}
