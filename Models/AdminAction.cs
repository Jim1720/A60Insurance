using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks; 

namespace A60Insurance.Models
{
    public class AdminAction
    {

        // since not all fields are used; we allow spaces so
        // code needs to check 'if there' depending on specific
        // action.

        
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = " Customer Id must only contains letters or numbers and is required.")]
        public string CustomerId { get; set; }

        
        [RegularExpression("^[a-zA-Z0-9\\s]*$", ErrorMessage = " New Password must only contains letters or numbers and is required.")]
        public string NewPassword { get; set; }

      
        [RegularExpression("^[a-zA-Z0-9\\s]*$", ErrorMessage = " Confirm New Password must only contains letters or numbers and is required.")]
        public string NewPassword2 { get; set; }

        
        [RegularExpression("^[a-zA-Z0-9\\s]*$", ErrorMessage = " New Customer Id must only contains letters or numbers and is required.")]
        public string NewCustomerId { get; set; } 

         
        [RegularExpression("^[a-zA-Z0-9\\s]*$", ErrorMessage = " Confirm Customer Id must only contains letters or numbers and is required.")]
        public string NewCustomerId2 { get; set; }
    }
}
