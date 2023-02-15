using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A60Insurance.Models
{

    public partial class CustomerList
    {
        public List<Customer> customers;

        public CustomerList()
        {
            customers = new List<Customer>();
        }

        public List<Customer> GetList()
        {
            return customers;
        } 

        public Customer GetCustomer(int Index)
        {
            return customers[Index];
        }
    }



    public partial class Customer
    {

        public Customer()
        {

            CustId = string.Empty;
            CustFirst = string.Empty;
            CustLast = String.Empty;
            CustMiddle = string.Empty;
            CustPassword = string.Empty;
            ConfirmPassword = string.Empty;
            Encrypted = string.Empty;
            CustEmail = string.Empty;
            CustBirthDate = DateTime.Now;
            CustGender = string.Empty;
            CustPhone = string.Empty;
            CustAddr1 = string.Empty;
            CustAddr2 = string.Empty;
            CustCity = string.Empty;
            CustState= string.Empty;
            CustZip= string.Empty;
            CustPlan = string.Empty;
            PromotionCode= string.Empty;
            AppId= string.Empty;
            ExtendColors = string.Empty;
            ClaimCount = string.Empty;
            ScreenBirthDate = string.Empty; 

        }

     public int Id { get; set; }

     [Required,
      RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = " Customer ID must only contains letters or numbers - required.")]
      public string CustId { get; set; }

        /* controllor logic may remember last keyed pw so blank is ok to pass model edit on pw. */
        /* if pw matches confirm user does not have to re enter if edit occurs convience for user*/
        /* it is not required again if edits occur - remove 'requited' ok. */
     
    [RegularExpression("^\\s|[a-zA-Z0-9\\s]*$", ErrorMessage = " password must only contains letters or numbers - required.")]
    public string CustPassword { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ConfirmPassword { get; set; }

    public string Encrypted { get; set; }

    [Required,
     RegularExpression("^[a-zA-Z0-9.\\s]*$", ErrorMessage = " First Name must only contains letters or numbers and is required.")]
    public string CustFirst { get; set; }

    [RegularExpression("^[a-zA-Z0-9.\\s]*$", ErrorMessage = " Middle Name must only contains letters or numbers and is required.")]

    public string CustMiddle { get; set; }

    [Required,
     RegularExpression("^[a-zA-Z.\\s]*$", ErrorMessage = "Last Name must contains letters and is required")]
    public string CustLast { get; set; }

    [Required]
    public string  CustEmail { get; set; }
    // edit date routine edits this field. screen uses screenBirthDate below.
    // once edited put data here.
    public DateTime CustBirthDate { get; set; }

    [Required,
        RegularExpression("^[mfMF]+$", ErrorMessage = "Gender m f")]

    public string CustGender { get; set; }
    [Required,
      Phone]
    public string CustPhone { get; set; }
    [Required,
       RegularExpression("^[a-zA-Z0-9#.\\s]*$", ErrorMessage = " address1 must only contains letters or numbers . # - required.")]
    public string CustAddr1 { get; set; }
    [RegularExpression("^\\s|[a-zA-Z0-9#.\\s]*$", ErrorMessage = "address2 must contains letters or numbers . #  if entered")]
    public string CustAddr2 { get; set; }
    [Required,
      RegularExpression("^[a-zA-Z.\\s]*$", ErrorMessage = "City must contains letters and is required")]
    public string CustCity { get; set; }
     public string CustState { get; set; }
    [Required]
    [RegularExpression("^[0-9]*$", ErrorMessage = "US zip must only contains numbers and is required.")]

    public string CustZip { get; set; }
    public string CustPlan { get; set; }
    public string PromotionCode { get; set; }
    public string AppId { get; set; }
    public string ExtendColors { get; set; }
    public string ClaimCount { get; set; }
         
    public string ScreenBirthDate { get; set; }
    }
}
