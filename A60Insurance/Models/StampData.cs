using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A60Insurance.Models
{
    public class StampData
    {
        public string AdjustedClaimId { get; set; }
        public string AdjustingClaimId { get; set; }
        public DateTime DateAdjusted { get; set; } 
        public string AppAdjusting { get; set; }

        public StampData(string pAdjustedId, string pAdjustingId, DateTime pDateAdjusted,
               string pAppAdjusting)
        {
            this.AdjustedClaimId = pAdjustedId;
            this.AdjustingClaimId = pAdjustingId;
            this.DateAdjusted = pDateAdjusted;
            this.AppAdjusting = pAppAdjusting;

        }
    }
}
