using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A60Insurance.Models
{
    public class DateParm
    { 
        public string    Input { get; set; }
        public string    Screen { get; set; } 
        public bool      Valid { get; set; } 
        public DateTime    Formatted { get; set; } 
        public string    Message { get; set; } 
    }
}
