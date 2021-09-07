using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using A60Insurance.Models;

namespace A60Insurance 
{
    public class EditDate
    {

        private int currentYear = 0;
        private int currentCentury = 0;
        private int currentYear2Digits = 0; 

        public void EditTheDate(DateParm dateParm)
        {
            dateParm.Valid = true;

            SetUp(dateParm);
            if(dateParm.Valid == false)
            {
                return;
            }

            // remove slashes
            var regularDate = this.IntakeSlashes(dateParm.Input); 
           
            var numeric = Int32.TryParse(regularDate,  out _ );
            if (!numeric)
            {
                dateParm.Valid = false;
                dateParm.Message = "Date not numeric.";
                return;
            }
            var dateLength = regularDate.Length;
            if(dateLength != 6 && dateLength != 8)
            {
                dateParm.Valid = false;
                dateParm.Message = "date invalid format";
                return;
            }

            var mm = regularDate.Substring(0, 2);
            var dd = regularDate.Substring(2, 2); 
            var yy = regularDate.Substring(4); // yy or yyyy works for 6 or 8.

            if (!EditMonth(mm))
            {
                dateParm.Valid = false;
                dateParm.Message = "date month invalid";
                return;
            }

            if (!this.EditYear(yy, dateParm.Screen))
            {
                dateParm.Valid = false;
                dateParm.Message = "date year invalid";
                return;
            }

            if (!this.EditDay(dd, mm, yy))
            {
                dateParm.Valid = false;
                dateParm.Message = "date day invalid";
                return;
            }

            const int needCenturyAddded = 2;
            if (yy.Length == needCenturyAddded)
            {
                var century = AddCentury(yy);
                yy = century + yy;
            }

            var y = Int32.Parse(yy);
            var m = Int32.Parse(mm);
            var d = Int32.Parse(dd); 
            
            DateTime formattedDate = new DateTime(y, m, d);

            dateParm.Valid = true;
            dateParm.Formatted = formattedDate;
            return;
        }

        protected void SetUp(DateParm dateParm)
        {
            // edit screen
            var validScreen = dateParm.Screen == "register"
                           || dateParm.Screen == "update"
                           || dateParm.Screen == "claim";

            if (!validScreen)
            {

                dateParm.Message = "invalid screen type.";
                dateParm.Valid = false;

            }

            if (dateParm.Input == null)
            {

                dateParm.Message = "date is invalid.";
                dateParm.Valid = false;

            }

            DateTime dt = DateTime.Now;
            this.currentYear = dt.Year;
            this.currentCentury = Int32.Parse(dt.Year.ToString().Substring(0,2));
            this.currentYear2Digits = Int32.Parse(dt.Year.ToString().Substring(2,2)); 
        }

        protected string AddCentury(string yy) 
        {
            // TODO: det impact on lciam dates timing of prior edits... 
            // we need to add century
            var inputYearWithoutCentury = Int32.Parse(yy);
            var nextYear = this.currentYear2Digits + 1;
            var useCentury = (inputYearWithoutCentury > nextYear) ? "19" : "20";
            return useCentury;
        }

        protected string IntakeSlashes(string DateToEdit)
        {
            // allow for dates like
            // 1/1/20:  
            // rules. find 2 slashes
            // pad m,d with 0 if good rules
            // remove slashes leave year untouched
            // if fail rules just return origional value.

            // example: 1/1/20 gives 010120

            var slash = "/";
            var dateItems = DateToEdit.Split(slash);
            var haveEnoughItems = 3;
            if (dateItems.Length < haveEnoughItems)
            {
                // no dashes or too few terms in date ; return date.
                return DateToEdit;
            }

            // m d y are in the dateItems array. pad m, d if needed...
            if (dateItems[0].Length == 1) { dateItems[0] = "0" + dateItems[0]; }; // pad m if 1 digit
            if (dateItems[1].Length == 1) { dateItems[1] = "0" + dateItems[1]; }; // pad d if 1 digit

            // do nothing with the year 

            // return mmdd(year) without slashes.

            return dateItems[0] + dateItems[1] + dateItems[2];
        }

        protected bool EditMonth(string Input)
        { 
            var month = int.Parse(Input);
            var monthValid = month >= 1 && month <= 12;
            return monthValid; 
        }

        protected bool EditDay(string mm, string dd, string yy)
        { 

            int [] thirtyMonth  = { 4, 6, 9, 11 };
            var day = int.Parse(dd);
            var month = int.Parse(mm);
            var year = int.Parse(yy);
            const int feb  = 2; 
             
            var dayLimit = 31;

            if (Array.IndexOf(thirtyMonth, mm) > -1) 
            {
                dayLimit = 30;
            }
            if (month == feb)
            { 
                dayLimit = (year % 4 == 0) ? 29 : 28;
            }
            var dayValid = (day > 0 && day <= dayLimit);
            return dayValid;
        }

        protected bool EditYear(string Input, string FromScreen)
        {
            var year = int.Parse(Input);
            // reasonable check only.
            var len = Input.Length;
            var validLength = len == 2 || len == 4;
            if (!validLength) { return false; }
            const int centuryOmitted = 2;

            if (len == centuryOmitted)
            {

                // registration can be any year since it is birth date
                // claim dates can be +1/-1 current year only
                // correspond with screen input...
                if (FromScreen == "claim" && (year < 19 || year > 21))
                {
                    return false;
                }
                return true;

            }

            const int dateIncludesCentury = 4;
            if (len == dateIncludesCentury)
            { 
                const int earlyLimit = 1900;
                var currentYear = this.currentYear;
                var lastYear = currentYear - 1;
                var nextYear = currentYear + 1;
                // procedure dates
                if (FromScreen == "claim" && (year < lastYear || year > nextYear))
                {
                    return false;
                }
                // birth date
                if ((FromScreen == "register" || FromScreen == "update")
                   && (year < earlyLimit || year > currentYear))
                {
                    return false;
                }
                return true;
            }

            // should never get here - for the compiler
            return false;

        }
    }
     
}
