using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A60Insurance.Models
{

    public partial class Services
    {
        public List<ServiceEntry> serviceList;

        public Services(List<ServiceEntry> input)
        {
            serviceList = new List<ServiceEntry>(input);
        }

        public List<ServiceEntry> GetList()
        {
            return serviceList;
        }

        public ServiceEntry GetServiceList(int Index)
        {
            return serviceList[Index];
        }
    }

    public class ServiceEntry
    {

        public string ServiceName { get; set; }
        public string ClaimType { get; set; }

        public string Cost { get; set; } 
    }

}
