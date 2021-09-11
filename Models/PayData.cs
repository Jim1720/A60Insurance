using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A60Insurance.Models
{
    public class PayData
    {
        public string ClaimId { get; set; }
        public decimal Amount { get; set; }

        public string Date { get; set; }

        public PayData(string ClaimId, decimal Amount, DateTime date)
        {
            this.ClaimId = ClaimId;
            this.Amount = Amount;
            this.Date = date.ToString("g");  // var must be caps or wont serialize!
        }
    }
}
