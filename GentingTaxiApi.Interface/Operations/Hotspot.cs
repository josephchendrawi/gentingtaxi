using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack;
using GentingTaxiApi.Interface.Types;

namespace GentingTaxiApi.Interface.Operations
{
    class Hotspot
    {
    }

    [Route("/gethotspot", "GET")]
    public class GetAllHotspotRequest : IReturn<HotSpotResponse>
    {
        public string token { get; set; }
    }

    [Route("/matchhotspot", "POST")]
    public class MatchHotspotRequest : IReturn<MatchHotspotResponse>
    {
        public string token { get; set; }
        public string from_name { get; set; }
        public string to_name { get; set; }
        public string frompx { get; set; }
        public string frompy { get; set; }
        public string topx { get; set; }
        public string topy { get; set; }
    }

    public class HotSpotResponse : Response
    {
        public List<HotspotVM> result { get; set; }
    }

    public class MatchHotspotResponse : Response
    {
        public decimal price { get; set; }
    }
}
