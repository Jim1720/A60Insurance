using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A60Insurance
{
    public interface IActionInformation
    {
        public void setAction(string Action, string ClaimIdNumber);

        public (string Action, string ClaimIdNumber) getAction(int number);
    }
    public class ActionInformation : IActionInformation
    {
        private List<SpecificAction> actionList;

        public ActionInformation()
        {
            actionList = new List<SpecificAction>();
        }
 

        public void setAction(string Action, string ClaimIdNumber)
        {
            SpecificAction toAdd = new SpecificAction(Action, ClaimIdNumber);

            var entries = actionList.Count();
            var keepListAtTwoEntries = entries > 2;
            if(keepListAtTwoEntries)
            {
                var firstEntry = 0;
                actionList.RemoveAt(firstEntry); // keep latest.
            }

            actionList.Add(toAdd);
            
        }


         public (string Action, string ClaimIdNumber) getAction(int number)
        {

            var validRequest = number == 1 || number == 2; 

            if (validRequest == false || number > actionList.Count)
            {
                return ("", "");
            }

            var entry = number - 1;
            SpecificAction requestedEntry = actionList[entry];
            var action = requestedEntry.action;
            var claim = requestedEntry.claimIdNumber;

            return (claim, action); 
        } 
}

    public class SpecificAction
    {
        public string action { get;  }
        public string claimIdNumber { get;  }

        public SpecificAction(string Action, string ClaimIdNumber)
        {
            action = Action;
            claimIdNumber = ClaimIdNumber;
        }
        
    }


}
