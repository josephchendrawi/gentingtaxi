using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class GeneralTypes
    {
    }

    public class Response
    {
        public int sts { get; set; }
        public string msg { get; set; }
        public string token { get; set; }
    }

    public enum PushDeviceType
    {
        android = 1 , 
        ios = 2 
    }
}
