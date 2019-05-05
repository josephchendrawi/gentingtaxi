using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GentingTaxiApi.Interface.Types
{
    class CountertxnTypes
    {
    }

    public class CounterTxnVM
    {
        public int counter_txnid { get; set; }
        public DateTime txn_datetime { get; set; }
        public string txn_countername { get; set; }
        public string ref_num { get; set; }
        public int bookingid { get; set; }
    }
}
