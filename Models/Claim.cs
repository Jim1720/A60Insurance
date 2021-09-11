using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A60Insurance.Models
{
    public partial class ClaimsHistory
    {
        public List<Claim> HistoryClaims;

        public ClaimsHistory()
        {
            HistoryClaims = new List<Claim>();
        }

        public List<Claim> GetList()
        {
            return HistoryClaims;
        }

        public Claim GetClaim(int Index)
        {
            return HistoryClaims[Index];
        }
    }

    // model   ServiceOptions.ClaimServiceList<ServiceEntry>
    public partial class ServiceOptions
    {
        // form points to this entry / it's hidden on the form / javascript selects
        // from this at startup and at 'claim type change' events.
         

        public List<SelectListItem> MedicalList;

        public List<SelectListItem> DentalList; 

        public List<SelectListItem> VisionList; 

        public List<SelectListItem> DrugList;

        public List<SelectListItem> GetServicesForType(String ClaimType)
        {

            switch(ClaimType)
            {
                case "m": return MedicalList;
                case "d": return DentalList;
                case "v": return VisionList;
                case "x": return DrugList;
                default: return null; 

            }

        }
 

        // calls this from Home controller at start up 1 time. Pass environment value.
        // return code: OK or message.
        public string  LoadServiceOptions(string ServiceFileName)
        {
       
             
            try
            { 
                string[] lines = System.IO.File.ReadAllLines(ServiceFileName);
                foreach (var line in lines)
                {
                    string[] entry = line.Split(',');
                    // ServiceName,ClaimType,Cost
                    string inputType = entry[1]; // Medical, Dental etc. change to m,d,v etc.
                    string useType = "";

                    switch(inputType)
                    {
                        case "Medical": useType = "m"; break;
                        case "Dental": useType = "d"; break;
                        case "Vision": useType = "v"; break;
                        case "Drug" : useType = "u"; break;
                        default: useType = "u"; break;
                    }

                    var name = entry[0];
                    var cost = entry[2];
                     

                    if(useType == "m")
                    {
                        MedicalList.Add(new SelectListItem { Text = name, Value = name });
                    }
                    if (useType == "d")
                    {
                        DentalList.Add(new SelectListItem { Text = name, Value = name });
                    }
                     if (useType == "d")
                    {
                        VisionList.Add(new SelectListItem { Text = name, Value = name });
                     }
                    if (useType == "d")
                    {
                        DrugList.Add(new SelectListItem { Text = name, Value = name });
                    }

                }

                return "OK";

            }
            catch (System.Exception ex)
            {
                return ex.Message.ToString();
               
            }

        }


    }

    public partial class Claim
    {
        public int Id { get; set; }
        public string ClaimIdNumber { get; set; }
        [Required,
       RegularExpression("^[ a-zA-Z0-9/s]*$",
       ErrorMessage = "Invalid claim description")]
        public string ClaimDescription { get; set; }
        public string CustomerId { get; set; }
        public string PlanId { get; set; }

        [Required,
         RegularExpression("^[a-zA-Z0-9/s]*$", 
         ErrorMessage = "Invalid Patient First Name")] 
        public string PatientFirst { get; set; }

        [Required,
        RegularExpression("^[a-zA-Z0-9/s]*$",
        ErrorMessage = "Invalid Patient Last Name")]
        public string PatientLast { get; set; }

        [Required,
        RegularExpression("^[0-9]*$",
        ErrorMessage = "Invalid Diagnosis")]
        public string Diagnosis1 { get; set; }
        public string Diagnosis2 { get; set; }
        [Required,
       RegularExpression("^[0-9]*$",
       ErrorMessage = "Invalid claim procedure")]
        public string Procedure1 { get; set; }
        public string Procedure2 { get; set; }
        public string Procedure3 { get; set; }
        [Required,
        RegularExpression("^[ a-zA-Z0-9/s+/.]*$",
        ErrorMessage = "Invalid Physician")]
        public string Physician { get; set; }
        [Required,
        RegularExpression("^[ a-zA-Z0-9/s+/.]*$",
        ErrorMessage = "Invalid Clinic")]
        public string Clinic { get; set; }
        public DateTime? DateService { get; set; }
        public string Service { get; set; }

        [NotMapped]
        public string dService { get; set; }

        [NotMapped]
        public string vService { get; set; }

        [NotMapped]
        public string xService { get; set; }


        public string ServiceItem { get; set; } 

        public string PaymentPlan { get; set; }
        public string Location { get; set; }

        public decimal TotalCharge { get; set; }
        public decimal CoveredAmount { get; set; }
        public decimal BalanceOwed { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? DateAdded { get; set; }
        public string AdjustedClaimId { get; set; }
        public string AdjustingClaimId { get; set; }
        public DateTime? AdjustedDate { get; set; }
        public string AppAdjusting { get; set; }
        public string ClaimStatus { get; set; }
        public string Referral { get; set; }
        public string PaymentAction { get; set; }
        public string ClaimType { get; set; }
        public DateTime? DateConfine { get; set; }
        public DateTime? DateRelease { get; set; }
        public int ToothNumber { get; set; }
        public string DrugName { get; set; }
        public string Eyeware { get; set; }

        // edited fields - not updated

        [NotMapped]
        public string ScreenDateService { get; set; }
        [NotMapped]
        public string ScreenDateConfine { get; set; }
        [NotMapped]
        public string ScreenDateRelease { get; set; }

        // show 1 of 4 dropdowns depending on claim type selected.

        [NotMapped]
        public List<SelectListItem> MedicalServiceOptions { get; set; }

        [NotMapped]
        public List<SelectListItem> DentalServiceOptions { get; set; }

        [NotMapped]
        public List<SelectListItem> VisionServiceOptons { get; set; }

        [NotMapped]
        public List<SelectListItem> DrugServiceOptions { get; set; }

        [NotMapped]
        public string firstMedical { get; set; }

        [NotMapped]
        public string firstDental { get; set; }

        [NotMapped]
        public string firstVision { get; set; }

        [NotMapped]
        public string firstDrug { get; set; }

        [NotMapped]
        public string pickedMedical { get; set; }

        [NotMapped]
        public string pickedDental { get; set; }

        [NotMapped]
        public string pickedVision { get; set; }

        [NotMapped]
        public string pickedDrug { get; set; }




    }
}
