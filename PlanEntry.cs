using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace A60Insurance.Models
{  

    public partial class Plans
    {
        public List<PlanEntry> planList;

        public Plans(List<PlanEntry> input)
        {
            planList = new List<PlanEntry>(input);
        }
         

        public List<PlanEntry> GetList()
        {
            return planList;
        }

        public PlanEntry GetPlanEntry(int Index)
        {
            return planList[Index];
        }
    }

    public class PlanEntry
    {

        public string PlanName { get; set;  }
        public string PlanLiteral { get; set; }

        public string Percent { get; set; }
         
    }
     
    
}
