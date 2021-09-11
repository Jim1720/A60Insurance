using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks; 

namespace A60Insurance.Models
{
    public class AdminSignin
    {
        [Required,
         RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = " Admin ID must only contains letters or numbers and is required.")]
        public string AdminId { get; set; }

        [Required,
         RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = " Password must only contains letters or numbers and is required.")]
        public string Password { get; set; }
 

    }
}
