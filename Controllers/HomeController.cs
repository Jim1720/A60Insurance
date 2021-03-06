using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using A60Insurance.Models;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.Extensions.Configuration; 
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

// Release 2 - 
// jsoncustomer - have update read this based on past cust id. to save tempdata space.

namespace A60Insurance.Controllers
{
    public class FetchParm
    {
        public bool status;
        public string message;
        public Customer signinCust;
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; 
        private readonly System.Net.Http.IHttpClientFactory _factory;
        // pull this in from env or something?
        private  string _send;
        //TODO: overposting, https and anti-forgeries 
        //TODO: authentication authorization 
        private string _admId;
        private string _admPassword;
        private string _promotionCode;
        private string _authEmail; 
        

        private readonly ITagHelperComponentManager _tagHelperComponentManager;
        private readonly IConfiguration _configuration;

        public HomeController(System.Net.Http.IHttpClientFactory factory,
                              ILogger<HomeController> logger,
                              ITagHelperComponentManager tagHelperComponentManager,
                              IConfiguration configuration) 
        { 
            _logger = logger;
            _factory = factory;
            _configuration = configuration;
            _logger.LogInformation("** Home Controller takes off!");
            _logger.LogInformation("** loading environment: ");

            // Load Environment values:
            _send = getVar("ApiUrlPrefix");
            _admId = getVar("A45AdmId");
            _admPassword = getVar("A45AdmPassword");
            _promotionCode = getVar("A45PromotionCode");
            _authEmail = getVar("A45EmailList");
            _logger.LogInformation("** environment loaded *");
                 
            _tagHelperComponentManager = tagHelperComponentManager;
            _logger.LogInformation("** Home Controller takes off!"); 
            //_logger.LogInformation("using url prefix for API calls: " + _send); 
        }

        protected string getVar(string key)
        {
            var value = _configuration.GetValue<string>(key, "");
            if(value == null)
            {
                var msg = "HomeController: no environment data for " + key;
                _ = new InvalidOperationException(msg);
            }
            return value;

        }

        public IActionResult Index()
        {
            _logger.LogInformation("*** showing index. ****");
            return View();
        }

        public IActionResult Classic()
        {
            return View();
        } 

        public IActionResult Menu()
        {

            var custId = TempData["CustomerId"]; 
            var msg = TempData["MenuMessage"];
            var token = TempData["Token"];

            TempData["Token"] = token;
            TempData["CustomerId"] = custId;
            TempData["CustomerSignedIn"] = "yes"; 
            TempData["MenuMessage"] = "";
            TempData.Keep();

            if(msg != null && msg.ToString() != "")
            { 

                ViewData["MenuMessage"] = msg;
            } 


           

            return View();
        }

        async public Task<IActionResult> Plan()
        {
             // Loads Plan entries 

            _tagHelperComponentManager.Components.Add(
               new PlanScreenBodyTagHelper());

            var plans =  (Plans) await ReadPlanListInformation();

            // 1. before we go replace CustomerId in temp data so it is 
            //    available on next method.

            var custId = TempData["CustomerId"].ToString();
            TempData["CustomerId"] = custId;
            TempData.Keep();

            return View(plans);
          
        } 

        async protected Task<Plans> ReadPlanListInformation()
        {
            var sendString = _send +
                           "api/readPlans";

            var client = _factory.CreateClient("read plans");
            _logger.LogInformation("* reading plans");
            var request = new HttpRequestMessage(HttpMethod.Get, sendString);

            //TODO: not: not add cors since in same project. 
            //Console.WriteLine("client preping for read plans");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1); 
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString());
                ViewData["Message"] = ex.Message.ToString();
            }
            _logger.LogInformation("good plan call");
            _logger.LogInformation("plan call completed.");

            String statusCode = response.StatusCode.ToString();
            Boolean goodResult = (statusCode == "OK");
            if (!goodResult)
            {
                ViewData["error"] = "Could not load plans";
                View("Error");
            }

            var input = await response.Content.ReadAsStringAsync(); 
            List<PlanEntry> planEntryList = JsonConvert.DeserializeObject<List<PlanEntry>>(input);

            Plans plans = new Plans(planEntryList); 
           
            return plans; 

        }



        [HttpPost]

        public async Task<IActionResult> PlanUpdate()
        {

            // there is no plan input on model , the clicked button puts data in hidden field read.

            // 1. get customer id. 

            var tCustId = TempData["CustomerId"];
            TempData.Keep();

            if (tCustId == null)
            {
                TempData["error"] = "Plan 01 - misadveture - id not set in claim setup.";
                return RedirectToAction("Error");
            } 
            var custId = tCustId.ToString().Trim();

            // workaround
            // var custId = "1";

            // 2. get selected plan from hidden form field.

            // process a plan button click - Plan Select Action.
            // Javascript sets hidden field.

            var defaultValue = "unused";

            Microsoft.Extensions.Primitives.StringValues selectedPlan;

            var getPlan = this.HttpContext.Request.Form.TryGetValue("hiddenPlan", out selectedPlan);

            var plan = selectedPlan[0].ToString();

            if (plan == defaultValue)
            {
                ViewData["error"] = "Plan select field value not read.";
                View("Error");
            }

            // 3. make call to update customer plan

            PlanUpdateParameters pup = new PlanUpdateParameters();
            pup.CustomerId = custId;
            pup.PlanName = plan;

            // update customer with selected plan name on button clicked.

            HttpResponseMessage m = await UpdateCustomerPlan(pup); ;

            if (m == null)
            {
                ViewData["error"] = "Could not update plan.";
                View("Error");
            }

            System.Net.HttpStatusCode statusCode = m.StatusCode;  
            Boolean goodResult = (statusCode == HttpStatusCode.OK ||
                statusCode == HttpStatusCode.NoContent); // TODO: check this out
            _logger.LogInformation("status code: " + statusCode);

            if (goodResult == false)
            {
                ViewData["error"] = "Plan update error.";
                View("Error");
            }

            TempData["MenuMessage"] = "Plan Updated.";
            TempData.Keep();

            return RedirectToAction("Menu");
        }

        

        private async Task<HttpResponseMessage> UpdateCustomerPlan(PlanUpdateParameters pup)
        {
            var uri = _send + "api/UpdateCustomerPlan/";
            var client = _factory.CreateClient("UpdateCustomerPlan");
            string json = JsonConvert.SerializeObject(pup);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
             {
                  RequestUri = new Uri(uri),
                  Content = content,
                  Method = HttpMethod.Put 
              };

            var token = TempData.Peek("Token").ToString(); 
            request.Headers.Add("A65TOKEN", token);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1);
            }
            catch (System.Exception ex)
            {

                _logger.LogInformation("caught:" + ex.Message.ToString());
            }
            _logger.LogInformation("update plan  call completed ok.");
            return response; 

        }




        public IActionResult Goals()
        {
            return View();
        } 

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Register()
        {
            TempData["anyMessage"] = ""; // clear message on update screen.
            TempData.Keep();
            ViewData["Message"] = "Welcome - to registration.";

            Customer customer = new Customer();

            return View(customer);
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> Register(Customer Customer)
        {
            _logger.LogInformation("** register being processed..."); 

            var custId = Customer.CustId as string;
            var pass   = Customer.CustPassword as string;
            var confirm = Customer.ConfirmPassword as string; 
          
            // basic edits
            ViewData["Message"] = "";


            /* normally facebook etc would be used
             * for email for this demo we use special values ...*/

            var email = Customer.CustEmail;
            if (email != null && _authEmail.IndexOf(email) > -1)
            {
                // good email
            }
            else
            {
                ViewData["Message"] = "A valid email address must be entered.";
                return View(Customer);
            }



            /*
            var emailPattern = "^([a-z0-9A-Z])(@)([a-z0-9A-Z])(\\.)([a-z0-9A-Z])$";
            if(Customer.CustEmail == null)
            {
                ViewData["Message"] = "A valid email address must be entered.";
                return View(Customer);
            }
            var email = Customer.CustEmail.Trim();
            var ematch = Regex.Match(email, emailPattern);
            if (!ematch.Success)
            {
                ViewData["Message"] = "Invalid Email";
                return View(Customer);
            } */


            // put screen date into edited field for edit and database update.
            // before checking valid state.
            DateParm dateParm = new DateParm();
            dateParm.Input = Customer.ScreenBirthDate;
            dateParm.Screen = "register";
            EditDate editDate = new EditDate();
            editDate.EditTheDate(dateParm);
            if(!dateParm.Valid)
            {
                ViewData["Message"] = dateParm.Message;
                return View(Customer);
            } 
            else
            {
                // joes in json customer to update screen via temp data to
                // to reduce 1 db call; signin does the same thing. so update
                // does not make a db call.
                Customer.CustBirthDate = dateParm.Formatted;
            }

            if(Customer.PromotionCode != _promotionCode)
            {
                ViewData["Message"] = "Invalid Promotion Code.";
                return View(Customer);

            }

            if (!this.ModelState.IsValid)
            {
                ViewData["Message"] = "Invalid Data Entered";
                return View(Customer);
            }
            int messageLength = ViewData["Message"].ToString().Length;

           
            Boolean editErrors = (messageLength > 0);
            if (editErrors)
            {
                return View(Customer);
            }

            // confirm password edit
            if(Customer.CustPassword != Customer.ConfirmPassword)
            {

                ViewData["Message"] = "Confirm Password does not match Password.";
                return View(Customer);
            }

            // put '' in for address2 if not keyed.
            var addr2 = (Customer.CustAddr2 == null) ? "" : Customer.CustAddr2;
            Customer.CustAddr2 = addr2;

            // constant fields
            Customer.AppId = "A60";
            Customer.Encrypted = "no";
            Customer.CustPlan = "";
            Customer.ClaimCount = "0";
            Customer.PromotionCode = "none";
            Customer.ExtendColors = "none";
            // end constant fields


            // duplicate customer check
            var IsDuplicate = await DuplicateCheck(custId);
            if(IsDuplicate)
            {

                ViewData["Message"] = "Duplicate Customer.";
                return View(Customer);
            }

            // make request to check customer and password. 
            HttpResponseMessage m = await RegisterCustomer(Customer);

            if (m == null)
            {
                ViewData["Message"] = "critical error - contact admin.";
                return View(Customer);
            }

            HttpStatusCode statusCode = m.StatusCode;
            // TO ADD:
            // if encrypt is on
            //    de encrypt all fields.
            // TO ADD:
            // ( test driven development - see requirements )
            //
            Boolean goodResult = (statusCode == HttpStatusCode.Created);
            _logger.LogInformation("status code: " + statusCode);

            if (goodResult == false)
            {
                ViewData["Message"] = "status code:" + statusCode + " was returned. ";
                return View(Customer);
            }

            // good result - save id and goto update  
            TempData.Clear();
            TempData["Token"] = GetToken(m);
            TempData["CustomerId"] = Customer.CustId;
            //string json = JsonConvert.SerializeObject(Customer);
            //TempData["jsonCustomer"] = json;
            TempData["existingPassword"] = Customer.CustPassword;
            //
            // show name at top.
            TempData["custFirst"] = Customer.CustFirst;
            TempData["custLast"] = Customer.CustLast;
            //
            TempData.Keep();
            return RedirectToAction("Plan");

        }

        private async Task<bool> DuplicateCheck(string custId)
        { 

            var sendString = _send +
                                       "api/Customer/" + custId;

            var client = _factory.CreateClient("read");
            _logger.LogInformation("* reading customer");
            var request = new HttpRequestMessage(HttpMethod.Get, sendString);

            //TODO: not: not add cors since in same project. 
            //Console.WriteLine("client preping for read customer dup check");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString());
                ViewData["Message"] = ex.Message.ToString();
            }
            _logger.LogInformation("good dup check  call");
            _logger.LogInformation("server call completed.");

            // return true if duplcate.
            return response.IsSuccessStatusCode;
        }
        

        private async Task<HttpResponseMessage> RegisterCustomer(Customer Customer)
        {

            // POST controller: route: api/Customers/  
             

            var uri = _send + "api/Customer/";
            var client = _factory.CreateClient("Register");
            string json = JsonConvert.SerializeObject(Customer);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Post
            }; 

            //TODO: not: not add cors since in same project. 
            _logger.LogInformation("client preping for register customer");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("put exception" + m1);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("caught:" + ex.Message.ToString());
            }

            TempData["Token"] = GetToken(response);
            TempData["CustomerSignedIn"] = true;
            TempData["AdminSignedIn"] = false;
            TempData["MenuMessage"] = "Customer Registered Successfully";
            TempData.Keep();

            _logger.LogInformation("register completed");
            return response;
        }
    

        public IActionResult Signin()
        {
            _logger.LogInformation("** signin displayed ***");
            TempData["anyMessage"] = ""; // clear message on update screen.
            TempData.Keep();
            ViewData["Message"] = "Welcome - to sign in.";

            Signin signin = new Signin(); 
            return View(signin);
        }

        // TODO: 
        // anti forgery - tag
        // overposting - list fields 

        [HttpPost]
        public async Task<ActionResult<Customer>> Signin(Signin signin)
        {
            _logger.LogInformation("** signin post being processed...");

            if (!ModelState.IsValid)
            {
                ViewData["Message"] = "invalid id or password.";
                return View(signin);
            }

            var custId = signin.CustomerId;
            var pass = signin.Password;

            HttpResponseMessage m =  await ReadCustomer(custId, "Signin");

            if( m == null )
            {
                ViewData["Message"] = "Server is down. Please report issue. Thanks."; 
                return View(signin);
            }

            if (m.StatusCode.ToString() == "InternalServerError" )
            {
                ViewData["Message"] =  "Server is up but has internal error";  
                return View(signin);
            }

            if (m.StatusCode.ToString() == "NotFound")
            {
                ViewData["Message"] = "Customer not found."; 
                return View(signin);
            }

            HttpContent content = m.Content;
            HttpStatusCode statusCode = m.StatusCode;
            string json = await content.ReadAsStringAsync();
            Customer customer = (Customer)JsonConvert.DeserializeObject<Customer>(json);

            Boolean goodResult = (statusCode == HttpStatusCode.OK);
            _logger.LogInformation("status code: " + statusCode);

            if (goodResult == false)
            {
                ViewData["Message"] += "status code:" + statusCode + " was returned. ";
                return View(signin);
            }

            // verify password entered
            Boolean goodPassword = (pass.Trim() == customer.CustPassword.Trim());
            if (!goodPassword)
            {
                ViewData["Message"] = "Incorrect password.";
                return View(signin);
            }

            // good result - save id and goto update.
            TempData.Clear();

            TempData["Token"] = GetToken(m);  
            TempData["signedIn"] = true;
            TempData["CustomerId"] = customer.CustId;
            TempData["CustomerName"] = customer.CustFirst + " " + customer.CustLast;
            TempData["jsonCustomer"] = json;
            TempData["custFirst"] = customer.CustFirst;
            TempData["custLast"] = customer.CustLast;
            TempData["custPass"] = customer.CustPassword;
            TempData["customerSignedIn"] = true; 
            TempData["AdminSignedIn"] = null; 

            // set up screen message.
            TempData["MenuMessage"] = "Customer Signed In.";

            TempData.Keep();

            return RedirectToAction("Menu");
        }

        private string GetToken(HttpResponseMessage response)
        {
            // save anti forgery token for future api calls...


            string token = "";
            foreach (var header in response.Headers)
            {
                if (header.Key == "Set-Cookie")
                {
                    token = "";
                    foreach (var s in header.Value)
                    {
                        token += s;
                    }
                };
            };

            return token;

        }

        private void AddTokenToRequest()
        {


        }

        public IActionResult Signout()
        {
            _logger.LogInformation("** signout **");
            TempData["CustomerId"] = null;
            TempData["CustomerName"] = null;
            TempData["signedIn"] = false;
           // TempData["jsonCustomer"] = null;
            TempData["custFirst"] = null;
            TempData["custLast"] = null;
            TempData["custPass"] = null;
            TempData["CustomerSignedIn"] = null;
            TempData.Keep();

            return View("Index");
        }
         

        

        private async Task<HttpResponseMessage> ReadCustomer(string custId, string Action) 
        { 

            var signinString = _send +
                            "api/Signin/" + custId;


            var readString = _send +
                           "api/Customer/" + custId;

            //signin gets a token; read does not.
            var sendString = (Action == "Signin") ? signinString : readString;


            var client = _factory.CreateClient("read");
            _logger.LogInformation("* reading customer");
            var request = new HttpRequestMessage(HttpMethod.Get, sendString);

            //TODO: not: not add cors since in same project. 
            //Console.WriteLine("client preping for read customer");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString());
                ViewData["Message"] = ex.Message.ToString();
            }
            _logger.LogInformation("good signin call");
            _logger.LogInformation("server call completed.");
            return response;
        }


        [HttpGet]
        public async Task<IActionResult> Update()
        {

            // verify proper signin
            var a = TempData["CustomerSignedIn"];
            TempData.Keep();
            if (a == null)
            {
                ViewData["error"] = "Update - not signed in.";
                return RedirectToAction("error");
            }
             
            /* verify important CustomerId is present. */
            var b = TempData["CustomerId"];
            TempData.Keep();
            if (b == null)
            {
                Console.Write("***** warning cust id is null");
                ViewData["error"] = "Update - CustomerID not present in view data.";
               // return RedirectToAction("error");
            }

            var customerId = b.ToString();

            // note: Cust Model has no customer id. 
            //String json = TempData["jsonCustomer"] as string;
            //Customer signinCust = (Customer)JsonConvert.DeserializeObject<Customer>(json);


            var (status, message, signinCust) =  await FetchCustomer(customerId);
           
            if(!status)
            {
                ViewData["error"] = message;
                View("Error");
            } 

            Cust cust = new Cust();

            // cust does not have id, password
            // used for screen validation only.
            // then, transimitted to customer to
            // update dateabase.
              
           cust.CustFirst = signinCust.CustFirst.Trim();
           cust.CustLast = signinCust.CustLast.Trim();
           cust.CustEmail = signinCust.CustEmail.Trim();
           cust.CustAddr1 = signinCust.CustAddr1.Trim();
           var value = (signinCust.CustAddr2 == null) ? "" : signinCust.CustAddr2;
           cust.CustAddr2 = value.Trim();
           cust.CustPhone = signinCust.CustPhone.Trim();
           cust.CustCity = signinCust.CustCity.Trim();
           cust.CustState = signinCust.CustState.Trim();
           cust.CustZip = signinCust.CustZip.Trim();
           cust.CustAddr1 = signinCust.CustAddr1.Trim();
           var value2 = (signinCust.CustMiddle == null) ? "" : signinCust.CustMiddle;
           cust.CustMiddle = value2.Trim(); 
           cust.CustGender = signinCust.CustGender.Trim();
           cust.CustPlan = signinCust.CustPlan.Trim();
           cust.ScreenBirthDate = DateOnScreen(signinCust.CustBirthDate); // edit attribute without time.
           var claimCount = signinCust.ClaimCount.Trim().ToString();
           var intCount = Int32.Parse(claimCount);

            TempData["existingPassword"] = signinCust.CustPassword;
            TempData["CustomerId"] = signinCust.CustId; 

            // show posted or standard message.
       //     var standard = cust.CustFirst + " " + cust.CustLast + " ready for update.";
      //      var anyMessage = TempData["anyMessage"].ToString();
        //    TempData.Keep(); 
       //     ViewData["Message"] = (anyMessage == "")  ? standard : anyMessage;

            var c = "";

            if (intCount == 0) { c = "no claims submitted."; };
            if (intCount == 1) { c = "one claim has been submitted."; };
            if (intCount > 1 ) { c = claimCount + " claims submitted."; };


            ViewData["ClaimInfo"] = c;

            // store non display fields in temp data to avoid losing data.


            var token = TempData["Token"];
            TempData["CustPlan"] = signinCust.CustPlan.Trim();
            TempData["ClaimCount"] = signinCust.ClaimCount.Trim();
        //*    TempData["Encrypted"] = signinCust.Encrypted.Trim();
            TempData["PromotionCode"] = signinCust.PromotionCode.Trim();
            TempData["AppId"] = signinCust.AppId.Trim();
            TempData["Token"] = token;
            TempData.Keep();

            return View(cust);
        } 

        async protected Task<(bool status, string message, Customer customer)>FetchCustomer(string CustomerId)
        {
            HttpResponseMessage m = await ReadCustomer(CustomerId, "Read");
            var message = "";
            var goodResult = false;

            if (m == null)
            {
                message = "Server is down. Please report issue. Thanks."; 
            }

                if (m.StatusCode.ToString() == "InternalServerError")
            {
                message = "Server is up but has internal error"; 
            }

            if (m.StatusCode.ToString() == "NotFound")
            {
                message = "Customer not found."; 
            }

            if(message != "")
            {
                return (false, message, null);
            } 

            HttpContent content = m.Content;
            HttpStatusCode statusCode = m.StatusCode;
            string json = await content.ReadAsStringAsync();
             goodResult = (statusCode == HttpStatusCode.OK);
            _logger.LogInformation("status code: " + statusCode);

            if (goodResult == false)
            {
                message += "status code:" + statusCode + " was returned. ";
            } 

            Customer customer = (Customer)JsonConvert.DeserializeObject<Customer>(json); 
            return (goodResult, message, customer);
        }

        protected string DateOnScreen(DateTime? date)
        {
            // change yyyy-mm-dd to mm/dd/yyyy
            string _date = date.ToString();
            int blankAt = _date.IndexOf(" ");
            _date = _date.Substring(0, blankAt);
            string[] items = _date.Split("/");
            // items 1/1/1900
            var y =  items[2];
            var m = (items[0].Length == 1) ? "0" + items[0] : items[0];
            var d = (items[1].Length == 1) ? "0" + items[1] : items[0];
            string result = m + "/" + d + "/" + y;
            return result; 
        }
         
        [HttpPost]
        public async Task<ActionResult<Cust>> Update(Cust Cust)
        {
           


            // screen built on cust model.
            // when valid data moved to customer with
            // id, rec id, password added to update data. 
            if ( TempData.Peek("CustomerId") == null)
            {
                ViewData["Message"] = "temp data missing.";
                return View(Cust);
            }
           

            // basic edits
            ViewData["Message"] = "";

            DateParm dateParm = new DateParm();
            dateParm.Input = Cust.ScreenBirthDate;
            dateParm.Screen = "update";
            EditDate editDate = new EditDate();
            editDate.EditTheDate(dateParm);
            if (!dateParm.Valid)
            {
                ViewData["Message"] = dateParm.Message;
                TempData.Keep();
                return View(Cust);
            }
            else
            {
                // joes in json customer to update screen via temp data to
                // to reduce 1 db call; signin does the same thing. so update
                // does not make a db call.
                Cust.CustBirthDate = dateParm.Formatted;
            }


            // Edits - edit screen...
            if (!this.ModelState.IsValid)
            {
                TempData.Keep();
                return View(Cust);
            }

            // Password - handle blank password
            String pass = Cust.CustPassword as string;
            if (pass == null || pass.Trim() ==  "") // not keyed copy existing....
            {
                // use existing one. 
                pass = TempData.Peek("existingPassword").ToString().Trim(); 
            }
            else
            {
                // make sure new one matches confirmation 
                if(Cust.ConfirmPassword == null)
                { 
                    ViewData["Message"] = "Please enter confirming password.";
                    TempData.Keep();
                    return View(Cust);
                }
                if(Cust.CustPassword != Cust.ConfirmPassword)
                { 
                    // check confirm to match.
                    ViewData["Message"] = "Confirm password does not match.";
                    TempData.Keep();
                    return View(Cust);
                }
            }

            // CustId -  not on screen, get it and put in customer object to use
            // since it is included in update data!!!
            var custId = TempData.Peek("CustomerId").ToString().Trim(); 
            if(custId == null)
            {
                ViewData["Message"] = "Msg01: please signout and try update again";
                TempData.Keep();
                return View(Cust);
            }  
 
            // now assemble cust , plus, id and password into customer object
            // to pass to update....

            Customer customer = new Customer();
            customer.CustId = custId;
            customer.CustPassword = pass;
            // -----
            customer.CustFirst = Cust.CustFirst;
            customer.CustLast = Cust.CustLast;
            customer.CustPhone = Cust.CustPhone;
            customer.CustEmail = Cust.CustEmail;
            customer.CustAddr1 = Cust.CustAddr1;
            customer.CustAddr2 = Cust.CustAddr2;
            customer.CustCity = Cust.CustCity;
            customer.CustState = Cust.CustState;
            customer.CustZip = Cust.CustZip;
            customer.CustGender = Cust.CustGender;
            customer.CustBirthDate = Cust.CustBirthDate;
            customer.CustMiddle = Cust.CustMiddle;

            // get fields saved in temp data.
            customer.CustPlan  = TempData["CustPlan"] as string; 
            customer.ClaimCount = TempData["ClaimCount"] as string; 
            customer.Encrypted = TempData["Encrypted"] as string; 
            customer.PromotionCode = TempData["PromotionCode"] as string;
            customer.AppId = TempData["AppId"] as string;
            TempData.Keep();

            HttpResponseMessage m = await UpdateCustomer(customer);

            if (m == null)
            {
                ViewData["Message"] = "critical error - contact admin.";
                TempData.Keep();
                return View(Cust);
            }

            System.Net.HttpStatusCode statusCode = m.StatusCode;
            // TO ADD:
            // if encrypt is on
            //    de encrypt all fields.
            // TO ADD:
            // ( test driven development - see requirements )
            //
            Boolean goodResult = (statusCode == HttpStatusCode.OK ||
                statusCode == HttpStatusCode.NoContent); // TODO: check this out
           _logger.LogInformation("status code: " + statusCode); 

            if(statusCode == HttpStatusCode.BadRequest)
            {
                ViewData["Message"] = "Invalid Access";
                TempData.Keep();
                return View(Cust);
            }

            if (goodResult == false)
            {
                ViewData["Message"] = "status code:" + statusCode + " was returned. ";
                TempData.Keep();
                return View(Cust);
            }

            // good result - save id and goto update 
            ViewData["Message"] = "Successful update.";

            // good result - update internal copy.
            //string json = JsonConvert.SerializeObject(customer);
            //TempData["jsonCustomer"] = json;
            TempData["existingPassword"] = customer.CustPassword;
            TempData.Keep(); 

            return View(Cust);
        }

         

        private async Task<HttpResponseMessage> UpdateCustomer(Customer customer)
        { 
            // Put controller.

            var uri = _send + "api/UpdateCustomer/";
            var client = _factory.CreateClient("update");
            string json = JsonConvert.SerializeObject(customer);
            var content = new StringContent(json,   
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);


            HttpResponseMessage response = null;
            try
            {
                //response = await client.PutAsync(uri, content);
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1);
            }
            catch (System.Exception ex)
            {

                _logger.LogInformation("caught:" + ex.Message.ToString());
            }
            _logger.LogInformation("update call completed ok.");
            return response;
        } 
        
        [HttpGet]
        public  async Task<ActionResult<Claim>> Claim()
        {

            // verify proper signin
            var a = TempData["CustomerSignedIn"];
            TempData.Keep();
            if (a == null)
            {
                return RedirectToAction("Index");
            }

            _logger.LogInformation("** claim displayed ***");
            ViewData["Message"] = "Welcome - please add claim."; 

            _tagHelperComponentManager.Components.Add(
                    new ClaimScreenBodyTagHelper());

            
            var tCustId = TempData.Peek("CustomerId"); 

            if (tCustId == null)
            {
                TempData["error"] = "Claim 01 - misadveture - id not set in claim setup.";
                return RedirectToAction("Error");
            }
            var custId = tCustId.ToString().Trim(); 

            _logger.LogInformation("claim load: cust id in temp data" + custId);

            var adjustedClaimId = TempData["adjustedClaimId"] as string;
            var paidClaimId = TempData["paidClaimId"] as string;
            // retain temp data values 
            TempData.Keep();

            var typeOfProcess = "new-claim";
            if(adjustedClaimId != null) { typeOfProcess = "create-adjustment"; };
            if (paidClaimId != null) { typeOfProcess = "pay-claim"; }; 
             


            if (typeOfProcess == "new-claim")
            {
                _logger.LogInformation("claim new claim for customer: " + custId);
                Models.Claim origionalClaim = new Claim();
                origionalClaim.ClaimType = "m"; // initialize claim type to medical.
                                                // new claim

                // for each drop down :
                // load hidden frontSeat fields (first row) ; java script
                //  will set 'orgionalClaim.Service' when type changes.
                // model: firstMedical, firstDental etc. so the
                // box will be set when type changes.

                // load services into Service list. 

                var taskResult = LoadServices();
                Services services = (Services) taskResult.Result;
                List<ServiceEntry> allServices = services.GetList();

                // default to Medical for new claim.
                List<ServiceEntry> typeServices = FilterByType(allServices, "m");
                var firstRow = 0;
                var row = typeServices[firstRow];
                origionalClaim.Service = row.ServiceName; // put first service in the drop down box.
               
                LoadDropDowns(allServices, origionalClaim);  


                //origionalClaim.ServiceOptions = typeServices.Select(ts => ts.ServiceName.ToString()).ToArray(); 
                return View(origionalClaim); 
            } 
           

            if (typeOfProcess == "create-adjustment")
            {
                _logger.LogInformation("claim adjust for claim: " + adjustedClaimId);

                HttpResponseMessage m = await GetClaim(adjustedClaimId);

                var msg = "";

                var code = "";

                if (m == null)
                {
                    code = "null response";
                }
                else
                {
                    code = m.StatusCode.ToString();
                }

                if (m == null || code != "OK")
                {
                    msg = "Critical error - contact admin  - code: " + code;
                }

                if(msg.Length > 0)
                {

                    Models.Claim claim = new Claim();
                    claim.ClaimType = "u"; // initialize claim type to medical.
                                                    // new claim
                    return View(claim);
                }

                HttpContent content = m.Content; 
                string json = await content.ReadAsStringAsync();
                Claim adjustedClaim = (Claim)JsonConvert.DeserializeObject<Claim>(json);

                // format service date - ScreenDateService

                adjustedClaim.ScreenDateService = FormatScreenDate(adjustedClaim.DateService.ToString());

                // format confine release dates - ScreenDateConfine, ScreenDateRelease

                if(adjustedClaim.ClaimType == "m")
                { 
                    adjustedClaim.ScreenDateConfine = FormatScreenDate(adjustedClaim.DateConfine.ToString()); 
                    adjustedClaim.ScreenDateRelease = FormatScreenDate(adjustedClaim.DateRelease.ToString());


                }

                adjustedClaim.Procedure1 = adjustedClaim.Procedure1.Trim();
                adjustedClaim.Procedure2 = adjustedClaim.Procedure2.Trim();
                adjustedClaim.Diagnosis1 = adjustedClaim.Diagnosis1.Trim();
                adjustedClaim.Diagnosis2 = adjustedClaim.Diagnosis2.Trim();
                adjustedClaim.Physician = adjustedClaim.Physician.Trim();
                adjustedClaim.Clinic = adjustedClaim.Clinic.Trim();
                adjustedClaim.Location = adjustedClaim.Location.Trim();
                adjustedClaim.PaymentAction = adjustedClaim.PaymentAction.Trim();
               

                var newAdjustmentId = DateTime.Now.ToString("CL-MM-dd-yy-H:mm:ss");
                TempData["newAdjustmentId"] = newAdjustmentId;

                // load services into Service list. 

                var taskResult = LoadServices();
                Services services = (Services)taskResult.Result;
                List<ServiceEntry> allServices = services.GetList();


                // drop down bound to claim.Service but typeServices provides
                // selection list for this claim type.

                List<ServiceEntry> typeServices = FilterByType(allServices, adjustedClaim.ClaimType);

                ViewData["message"] = "Enter adjusting claim for ";
                ViewData["message2"] = adjustedClaimId;
                ViewData["message3"] = "Adjustment claim id will be:";
                ViewData["message4"] = newAdjustmentId;

                // trim name
                adjustedClaim.PatientFirst = adjustedClaim.PatientFirst.Trim();
                adjustedClaim.PatientLast = adjustedClaim.PatientLast.Trim();
                //TODO: determine date handling for dates. 


                LoadDropDowns(allServices, adjustedClaim); 

                return View(adjustedClaim);
            }

            TempData["error"] = "Claim Screen: could not initiate adjustement...";
            TempData.Keep();
            return View("Error"); // should not come here.
        }
         

        private void LoadDropDowns(List<ServiceEntry> list, Claim claim)
        {
            claim.MedicalServiceOptions = new List<SelectListItem>();
            claim.DentalServiceOptions = new List<SelectListItem>(); 
            claim.VisionServiceOptons = new List<SelectListItem>();
            claim.DrugServiceOptions = new List<SelectListItem>(); 

            LoadDropDown(list, "m", claim);
            LoadDropDown(list, "d", claim);
            LoadDropDown(list, "v", claim);
            LoadDropDown(list, "x", claim); 

        }
        private void LoadDropDown(List<ServiceEntry> list, string shortType, Claim claim)
        {
            // put service names in a drop down box
            // set first field 

            // firstField = "";

            var selectedService = claim.Service.Trim();
            var type = claim.ClaimType;
            var preSelected = (selectedService != null) ? true : false;

            var options = from t in list
                                 where t.ClaimType == shortType
                                 select new { t.ServiceName };

            //var first = true;
            foreach (var item in options)
            {
                var name = item.ServiceName.Trim();
                SelectListItem sli = new SelectListItem();
                sli.Text = name.ToString().Trim();
                sli.Value = sli.Text;
                // determine selection 
                if (preSelected && selectedService == name && type == shortType)
                {
                    // INSERT pre=selected at top of list. (adjustments, new claim edits).
                    switch (shortType)
                    {
                        case "m": claim.MedicalServiceOptions.Insert(0, sli); break;
                        case "d": claim.DentalServiceOptions.Insert(0, sli); break;
                        case "v": claim.VisionServiceOptons.Insert(0, sli); break;
                        case "x": claim.DrugServiceOptions.Insert(0, sli); break;
                    }

                }
                else
                {
                    /* add items to list */
                    switch (shortType)
                    {
                        case "m": claim.MedicalServiceOptions.Add(sli); break;
                        case "d": claim.DentalServiceOptions.Add(sli); break;
                        case "v": claim.VisionServiceOptons.Add(sli); break;
                        case "x": claim.DrugServiceOptions.Add(sli); break;
                    }
                }


            }

        }

        private List<ServiceEntry> FilterByType(List<ServiceEntry> allServices, string claimType)
        {
            List<ServiceEntry> result = new List<ServiceEntry>();
            foreach(var s in allServices)
            {
                if(s.ClaimType == claimType)
                {
                    result.Add(s);
                }
            }
            return result;
        }

       

        async protected  Task<decimal> PlanPercent(string custPlan)
        {
            decimal percent = 0.0M; 

            var plans = (Plans)await ReadPlanListInformation(); 
            var list = plans.GetList();

            var row = list.Find(p => p.PlanName.Trim() == custPlan);
            var ok  = decimal.TryParse(row.Percent, out decimal value);
            if(ok) { percent = value;  }
            return percent;
        }

        protected  async Task<Services> LoadServices()
        {
            // // POST: api/Stamp 
            var uri = _send + "api/readServices/";
            var client = _factory.CreateClient("readServices");

            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(uri);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("get exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("get exception" + ex.Message.ToString());
            }

            String statusCode = response.StatusCode.ToString();
            Boolean goodResult = (statusCode == "OK");
            if (!goodResult)
            {
                ViewData["error"] = "Could not load services";
                View("Error");
            }
            // array of service entires are returned. Each entry has a Service Name, ClaimType, and Cost.
            // create object array from json entries in the list   list<string-of-json>. 
             

            var input = await response.Content.ReadAsStringAsync();
            List<ServiceEntry> serviceEntryList = JsonConvert.DeserializeObject<List<ServiceEntry>>(input);

            Services services = new Services(serviceEntryList);

            return services; 

        }
 

        protected async Task<HttpResponseMessage> GetClaim(String claimIdNumber)
        {
            // // POST: api/Stamp 
            var uri = _send + "api/Claim/" + claimIdNumber;
            var client = _factory.CreateClient("readclaim");  

            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(uri);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("get exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("get exception" + ex.Message.ToString());
            }

            String statusCode = response.StatusCode.ToString();
            Boolean goodResult = (statusCode == "OK");
            if (goodResult)
            {
                return response;
            } 
           
            return null; 
        } 

        [HttpPost]
        public async Task<ActionResult<Customer>> Claim(Claim claim)
        {
            _logger.LogInformation("** claim post being processed...for claim type:");
            _logger.LogInformation(claim.ClaimType);

            _tagHelperComponentManager.Components.Add(
                  new ClaimScreenBodyTagHelper());

            var dcustId = TempData["CustomerId"].ToString();
            TempData.Keep();
            _logger.LogInformation("claim proc: cust id in temp data" + dcustId);
            
            


            // pre edit dates and place values into model
            // before checking model validity.
            DateParm dateParm = new DateParm();
            dateParm.Input = claim.ScreenDateService; ;
            dateParm.Screen = "claim";
            EditDate editDate = new EditDate();
            editDate.EditTheDate(dateParm);
            if (!dateParm.Valid)
            {
                ViewData["Message"] = dateParm.Message;
                EditReloadDropdowns(claim); 
                return View(claim);
            }
            else
            {
                // joes in json customer to update screen via temp data to
                // to reduce 1 db call; signin does the same thing. so update
                // does not make a db call.
                claim.DateService = dateParm.Formatted;
            }

            // medical claim pre-edit confine and release dates.
            // nulls will default to defualt date later in code.

            if (claim.ClaimType == "m" && claim.ScreenDateConfine != null)
            {

                dateParm.Input = claim.ScreenDateConfine; ;
                dateParm.Screen = "claim";
                editDate.EditTheDate(dateParm);
                if (!dateParm.Valid)
                {
                    ViewData["Message"] = dateParm.Message;
                    EditReloadDropdowns(claim);
                    return View(claim);
                }
                else
                {
                    // joes in json customer to update screen via temp data to
                    // to reduce 1 db call; signin does the same thing. so update
                    // does not make a db call.
                    claim.DateConfine = dateParm.Formatted;
                }
            }


            if (claim.ClaimType == "m" && claim.ScreenDateRelease != null)
            {
                dateParm.Input = claim.ScreenDateRelease; ;
                dateParm.Screen = "claim";
                editDate.EditTheDate(dateParm);
                if (!dateParm.Valid)
                {
                    ViewData["Message"] = dateParm.Message;
                    EditReloadDropdowns(claim);
                    return View(claim);
                }
                else
                {
                    // joes in json customer to update screen via temp data to
                    // to reduce 1 db call; signin does the same thing. so update
                    // does not make a db call.
                    claim.DateRelease = dateParm.Formatted;
                }

            } /* end pre edits on dates*/

            /* map combo box for type value to claim.Service -- 
             * by defualt medical is mapped */

            var t = claim.ClaimType;
            var s = claim.Service;
            switch(t)
            {
                case "m": s = claim.Service; break; // redundant but readable.
                case "d": s = claim.dService; break;
                case "v": s = claim.vService; break;
                case "x": s = claim.xService; break;
            }
            claim.Service = s; 

            /* service edit */
            if(claim.Service == null)
            {
                ViewData["Message"] = "Please select service.";
                EditReloadDropdowns(claim);
                return View(claim);
            }

            ViewData["Message"] = "";
            if (!this.ModelState.IsValid)
            {
                ViewData["Message"] = "Invalid Data Entered";
                EditReloadDropdowns(claim);
                return View(claim);
            }
            var messageLength = ViewData["Message"].ToString().Length;
            if (messageLength > 0)
            {
                return View(claim);
            }

            var custId = TempData["CustomerId"].ToString();
            TempData.Keep();
            //
            claim.CustomerId = custId;  
            // todo : edit and set dates and total charge and claim id.
            // CL-mm-dd-ccyy-hh:mm 
            var date = DateTime.Now;

            var defaultDate = new DateTime(1753, 01, 01);
            var currentDate = DateTime.Now;

            claim.ClaimIdNumber = date.ToString("CL-MM-dd-yy-H:mm:ss");
            claim.ClaimStatus = "Entered";
            claim.AdjustedClaimId = "";
            claim.AdjustedDate = defaultDate;
            claim.DateAdded = date; 
            claim.ServiceItem = "";
            claim.PaymentDate = defaultDate;
            claim.PaymentAmount = 0; 
            claim.PaymentPlan = "none"; 
            claim.ServiceItem = "";
            claim.AppAdjusting = "";
            claim.Procedure3 = "";



            //TODO: put cust plan here 

            var (status, message, customer) = await FetchCustomer(custId); 

            if (!status)
            {
                ViewData["error"] = message;
                EditReloadDropdowns(claim);
                View("Error");
            }

            var plan = customer.CustPlan.Trim();

            // edit for valid plan.
            if (plan == "")
            {
                ViewData["Message"] = "You must select a plan before filing claim";
                EditReloadDropdowns(claim);
                return View(claim);
            }  

            claim.PlanId = plan; 

           /* switch(claim.ClaimType)
            {
                case "m": claim.Service = claim.pickedMedical; break;
                case "d": claim.Service = claim.pickedDental; break;
                case "v": claim.Service = claim.pickedVision; break;
                case "x": claim.Service = claim.pickedDrug; break;
            } */

            var longClaimType = "";
            switch (claim.ClaimType)
            {
                // match service data claim type literal.
                case "m": longClaimType = "Medical"; break;
                case "d": longClaimType = "Dental"; break; 
                case "v": longClaimType = "Vision"; break;
                case "x": longClaimType = "Drug"; break; 
            }


            (decimal cost, decimal covered, decimal balance) 
                = CalculateCostValues(plan, claim.Service, longClaimType);


            claim.TotalCharge = cost;
            claim.CoveredAmount = covered;
            claim.BalanceOwed = balance;


            if (claim.ClaimType == null)
            {
                // default claim type - no button clicked;
                claim.ClaimType = "m";
            }
            if(claim.ClaimType == "m")
            {
                if(claim.DateConfine == null) { claim.DateConfine = defaultDate;  }
                if(claim.DateRelease == null) { claim.DateRelease = defaultDate;  }  
                if(claim.DateService == null) { claim.DateService = currentDate;  }
            }


            // initialize unused fields for other claim types.
            if (claim.ClaimType != "m")
            {   claim.DateConfine = defaultDate;
                claim.DateRelease = defaultDate;
            }
            if (claim.ClaimType != "d")
            { claim.ToothNumber = 0; }
            if (claim.ClaimType != "v")
            { claim.Eyeware = ""; }
            if (claim.ClaimType != "x")
            { claim.DrugName = ""; }
            // end
            claim.AdjustedClaimId = "";
            claim.Procedure3 = ""; // do not use - room on screen

            string adjustedClaimId = TempData["adjustedClaimId"] as string; 
            // retain temp data values 
            TempData.Keep();
            // do any adjustment processing if needed.

            // 
            // are you lloking for payment logic ?
            // look at Get! It is there.


            var adjustment = (adjustedClaimId != null);

            // Timing: when both id's match - error out. 
            if (adjustedClaimId == claim.ClaimIdNumber)
            {
                ViewData["Message"] = "Adjustment has same claim id as original claim.";
                EditReloadDropdowns(claim);
                return View(claim);
            }
            //
            if (adjustment)
            {
                claim.ClaimIdNumber = TempData["newAdjustmentId"].ToString();
                TempData.Keep();
                claim.AdjustedClaimId = adjustedClaimId;
                claim.ClaimStatus = "Adjustment";
            }
         
            HttpResponseMessage m = await AddClaim(claim);
            if (m == null)
            {
                ViewData["Message"] = "critical error - contact admin.";
                EditReloadDropdowns(claim);
                return View(claim);
            }

            System.Net.HttpStatusCode statusCode = m.StatusCode;
            // TO ADD:
            // if encrypt is on
            //    de encrypt all fields.
            // TO ADD:
            // ( test driven development - see requirements )
            //
            Boolean goodResult = (statusCode == System.Net.HttpStatusCode.OK);
            _logger.LogInformation("status code: " + statusCode);

            if (goodResult == false)
            {
                ViewData["Message"] = "status code:" + statusCode + " was returned. ";
                EditReloadDropdowns(claim);
                return View(claim);
            }

            // stamp adjusted claim 
            var adjMessage = "";
            if(adjustment)
            {

                // do not use get,put claim just call A60 to stamp the claim. 
                
                // /StampClaim/{form}
                // ==================

                bool success = await StampClaim(adjustedClaimId,
                                   claim.ClaimIdNumber,
                                   date);
                if(!success)
                {
                    ViewData["Message"] = "Serious error : stamp failed.";
                    EditReloadDropdowns(claim);
                     View("Error");
                }
                // reset to null so claim screen is ok.
                TempData["adjustedClaimId"] = null;
                TempData.Keep();

                adjMessage = adjustedClaimId + " adjusted claim " + claim.ClaimIdNumber;

            }

            // The claim screen will post message for update screen once claim
            // filed or adjusted.

            
            var newClaimMessage  = "Claim " + claim.ClaimIdNumber + " posted."; 
            var useMessge = (adjMessage.Length > 0) ? adjMessage : newClaimMessage;
            TempData["MenuMessage"] = useMessge;
            TempData.Keep();

            int updatedClaimCount = await AddClaimCount(custId);  

            // return to main menu - hub.
            return RedirectToAction("menu");
        }

        protected void EditReloadDropdowns(Claim claim)
        {
            // edits: reload drop downs.
            var taskResult = LoadServices();
            Services services = (Services)taskResult.Result;
            List<ServiceEntry> allServices = services.GetList();
            LoadDropDowns(allServices, claim);
        }


        protected (decimal cost, decimal covered, decimal balance) 
               CalculateCostValues(string plan, string service, string claimType)
        {


            //TODO: get the custome plan from TempData or db.

            // find percent from current customer plan.
            // (the plan list may be held in core after first call.
            decimal percent = PlanPercent(plan).Result;

            // from service loaded before claim displayed 
            // find the service and cost of that service.
            // (this may be held in core after first call)
            decimal cost = Cost(service, claimType);

            decimal covered = cost * percent / 100;

            decimal balance = cost - covered;

            return (cost, covered, balance);

        }
        private decimal Cost(string Service, string claimType)
        {
            // load services into Service list. 

            var service = Service.Trim();
            var type = "";
            switch(claimType)
            {
                case "Medical": type = "m"; break;
                case "Dental": type = "d"; break;
                case "Vision": type = "v"; break;
                case "Drug": type = "d"; break;
            }

            var taskResult = LoadServices();
            Services services = (Services)taskResult.Result;
            List<ServiceEntry> allServices = services.GetList();
           
            decimal cost = 0.0M;
            for (var i = 0; i < allServices.Count; i++)
            {
                var sRow = allServices[i];
                var name = sRow.ServiceName.Trim();
                var cType = sRow.ClaimType;
                if (name == service && cType == type)
                {
                    cost = decimal.Parse(sRow.Cost);
                } 
            }; 
            
            return cost; 
        }

        private async Task<int> AddClaimCount(string custId)
        {
            // stamp adjusted claim was reading and writing claim. wow remove these please.

            var uri = _send + "api/AddClaimCount/" + custId; 
            var client = _factory.CreateClient("addClaimCount");  
            
            // AddClaimCount always adds 1 
            string json = JsonConvert.SerializeObject(custId);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            var caught = false;
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("put exception" + m1);
                caught = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("add count post exception" + ex.Message.ToString());
                caught = true;
            }
            _logger.LogInformation("add count completed");

            // -1 is returned if error check.
            int callResponse = -1;
            int intClaimCount = 0;
            string value = await response.Content.ReadAsStringAsync(); 
            if(caught == false && Int32.TryParse(value, out intClaimCount))
            {
                callResponse = intClaimCount;
            }
            return callResponse;

              
        }
        private async Task<Boolean> StampClaim(string adjustedClaimId, 
                                               string adjustingClaimId,
                                               DateTime date)
        {

            // stamp adjusted claim was reading and writing claim. wow remove these please.

            var uri = _send + "api/StampAdjustedClaim/";
            var client = _factory.CreateClient("stampClaim");
            var appAdjusting = "A60";

            StampData stampData = new StampData(adjustedClaimId, adjustingClaimId, date,
                appAdjusting);

            string json = JsonConvert.SerializeObject(stampData);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            var caught = false;
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("put exception" + m1);
                caught = true;
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("stamp claim post exception" + ex.Message.ToString());
                caught = true;
            }
            _logger.LogInformation("stamp claim completed");

            var code = response.StatusCode.ToString();

            var result = (caught == false && code == "OK");

            return result; 

        }

       
        private async Task<HttpResponseMessage> AddClaim(Models.Claim claim)
        {
            // // POST: api/Claim 
            var uri = _send + "api/claim/";
            var client = _factory.CreateClient("claimAdd");
            string json = JsonConvert.SerializeObject(claim);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Post
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("post exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("post exception" + ex.Message.ToString());
            }
            _logger.LogInformation("add claim completed");
            return response;
        }
         
        [HttpGet]
        public async Task<ActionResult<ClaimsHistory>>History()
        {

            // verify proper signin
            var a = TempData["CustomerSignedIn"];
            TempData.Keep();
            if (a == null)
            {
                return RedirectToAction("Home/Index");
            }

            var tCustId = TempData["CustomerId"].ToString().Trim();
            //workaround
            //tCustId = "1";
            TempData.Keep();

            if (tCustId == null)
            {
                TempData["error"] = "Hist 01 - misadveture - id not set in claim setup.";
                return RedirectToAction("Error");
            }
            var custId = tCustId.ToString();



            _tagHelperComponentManager.Components.Add(
                   new ClaimScreenBodyTagHelper());

            ClaimsHistory ch = await ReadClaimHistory(custId); 
            return View(ch);
              
        }

        private async Task<ClaimsHistory> ReadClaimHistory(string custId)
        {
             
 
            ClaimsHistory claimsHistory = new ClaimsHistory();

            if (custId == null)
            {
                ViewData["Message"] = "*01* history error - missing customer id.";
                return claimsHistory; // view name
            }

            var sendString = _send +
                           "History/" + custId;

            var client = _factory.CreateClient("history");
            var request = new HttpRequestMessage(HttpMethod.Get, sendString);

            ViewData["Message"] = "";
            HttpResponseMessage m = null;
            try
            {
                m = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("req exception" + m1);
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString());
                ViewData["Message"] = ex.Message.ToString();
            }
            _logger.LogInformation("good signin call");
            _logger.LogInformation("server call completed.");


            HttpContent content = m.Content;
            HttpStatusCode statusCode = m.StatusCode;

            if(m.StatusCode.ToString() == "NotFound")
            {
                ViewData["Message"] = "No Claims Found.";
            }

            if (ViewData["Message"].ToString() != "")
            {
                return claimsHistory; // view name - error condition
            }

            string json = await content.ReadAsStringAsync();  
            var resultData = JsonConvert.DeserializeObject<List<Claim>>(json); 
            foreach(var item in resultData)
            {
                Claim c = (Claim)item;
                claimsHistory.HistoryClaims.Add(c);
            }
            return claimsHistory;
        }

        protected (Boolean,string,string) ReadPaymentData()
        {
            // examine hidden paymment amount field to see if user clicked 'pay claim'.
            // java script puts claim id in the hideClaimId Field

            var defaultValue = "unused";
            var sAmount = "";
            var failed = (false, "", "");

            var payClaimClicked = false;  
            Microsoft.Extensions.Primitives.StringValues inAmount, inClaimId;

            var getAmount = this.HttpContext.Request.Form.TryGetValue("hiddenAmount", out inAmount); 
            var getClaimId = this.HttpContext.Request.Form.TryGetValue("hiddenClaimId", out inClaimId);

            var requiredFieldsInput = getAmount && getClaimId;
            if(!requiredFieldsInput)
            { 
                return failed;
            } 
             
            // if unused no amount was entered
            var amountEntered = (inAmount != defaultValue); 
            if(amountEntered)
            {
                // javascript verified value
                 sAmount = inAmount.ToString();
                 payClaimClicked = true;
            }
            else
            {
                return failed;
            }
            return (payClaimClicked, sAmount, inClaimId);
        }

        (Boolean isAdjustment, string ClaimIdNumber) ReadAdjustmentData()
        {
            var isAdjustment = false;
            var claimId = "";

            Microsoft.Extensions.Primitives.StringValues inAction; 
            var _ = this.HttpContext.Request.Form.TryGetValue("sub", out inAction);
            var item = inAction.ToString(); 
            if (item.Length <= 6)
            {
                return (false, "");
            }
             
            var verb = item.Substring(0, 6);
            if (verb == "adjust")
            {
                isAdjustment = true;
                claimId = item.Substring(6);
            }

            return (isAdjustment, claimId);

        }

        [HttpPost]
        public IActionResult ReadClaimHistory(ClaimsHistory ch)
        { 
         
            /* payment - the amount and claim stored in hidden fields at top because
             * the alert stops the submit values from being transmitted. */
              

            // 1. Examine payment fields.... set up by javascript InputPayment function.
            (Boolean paymentUsed, string paymentAmount, string payClaimId) = ReadPaymentData();

            // 2. Examine adjustment fields. 
            (Boolean isAdjustment, string AdjClaimId) = ReadAdjustmentData(); 

            var validRequest = paymentUsed || isAdjustment;
            if (!validRequest)
            { 
                ViewData["message"] = "could not process request..";
                RedirectToAction("History"); // reload model.
            }

            // all verbs must be six characters in length.
            // and be followed by integer index 1,2,3 way loop is coded.

            // on history screen: adjustment button disabled if 
            // claim already adjusted - same with pay button.
            if (isAdjustment)
            {
                TempData["adjustedClaimId"] = AdjClaimId;
                TempData.Keep();
                return RedirectToAction("Claim");
            }
            if (paymentUsed)
            { 

                // get payment amount - it was edited in the javascript for feedback to user...
                // so its 'unused' or good value.
                double payment = 0.0d; 
                var good = double.TryParse(paymentAmount,  out payment);
                if(!good)
                {
                    // just return to screen - edit was in java script.
                    RedirectToAction("History"); // reload model.
                } 
                string standardMessage = "Claim " + payClaimId + " paid " + paymentAmount + "."; 
                Task<HttpResponseMessage> m = PayClaim(payClaimId, payment.ToString());
                if (m == null)
                {
                    TempData["anyMessage"] = "Pay claim: critical error - contact admin.";
                    TempData.Keep();
                    return RedirectToAction("Menu");
                }

                System.Net.HttpStatusCode statusCode = m.Result.StatusCode;
                var goodResult = statusCode.ToString() == "OK";
                var anyMessage = "Pay:unexpected status code: " + statusCode.ToString();
                if(goodResult)
                {
                    var a = payClaimId;
                    var b = paymentAmount;
                    anyMessage = $"Claim {a} was paid with ${b}";

                }  
                TempData["MenuMessage"] = anyMessage;
                TempData.Keep();
                return RedirectToAction("Menu");
            }

            return View(ch);
        }
        protected async Task<HttpResponseMessage> PayClaim(string ClaimId, string Amount)
        {

            _logger.LogInformation("pay claim: " + ClaimId);
            // // PUT: api/PayClaim/{paydata} 
            var uri = _send + "api/PayClaim/";
            var client = _factory.CreateClient("payClaim");
            DateTime date = DateTime.Now;
            decimal _amount = decimal.Parse(Amount);
            PayData payData = new PayData(ClaimId, _amount, date);

            string json = JsonConvert.SerializeObject(payData);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                _logger.LogInformation("pay exception" + m1); 
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("pay exception" + ex.Message.ToString());
                 
            }

            return response;

        }

        public IActionResult AdminSignin()
        {
            ViewData["Message"] = "Welcome - to administration.";

            AdminSignin adminSignin = new AdminSignin();
            return View(adminSignin);
        }

        [HttpPost]
        public IActionResult AdminSignin(AdminSignin AdminSignin)
        {
            if(!ModelState.IsValid)
            {
                return View(AdminSignin);
            }

            if(AdminSignin.AdminId !=  _admId)
            {
                ViewData["Message"] = "Admin Signin is not valid.";
                return View(AdminSignin);
            }

            

            if (AdminSignin.Password != _admPassword)
            {
                ViewData["Message"] = "Admin password is not valid.";
                return View(AdminSignin);
            }

            ViewData["Message"] = "Welcome - to admin action.";

            TempData["AdminSignedIn"] = true;
            TempData["CustomerSignedIn"] = null;
            TempData.Keep();

            return RedirectToAction("AdminAction");
        }


        public IActionResult AdminAction()
        {
            // check for valid login
            var a = TempData["AdminSignedIn"];
            TempData.Keep();
            if(a == null)
            {
                return RedirectToAction("Index");
            }


            ViewData["Message"] = "Welcome - to admin action.";

            AdminAction adminAction = new AdminAction();
            return View(adminAction);
        }

        [HttpPost]
        public  IActionResult AdminAction(AdminAction AdminAction)
        {
            Microsoft.Extensions.Primitives.StringValues Action;
            var _ = this.HttpContext.Request.Form.TryGetValue("sub", out Action);
      
            // detect action
            if(Action == "ResetPassword")
            {
                if (AdminAction.CustomerId == null)
                {
                    ViewData["Message"] = "you must enter a customer id.";
                    return View(AdminAction);
                }
                if (AdminAction.NewPassword == null)
                {
                    ViewData["Message"] = "you must enter a new password";
                    return View(AdminAction);
                }
                if(AdminAction.NewPassword2 == null)
                {
                    ViewData["Message"] = "you must enter a new password";
                    return View(AdminAction);
                }
                if (AdminAction.NewPassword != AdminAction.NewPassword2)
                {
                    ViewData["Message"] = "Confirmation password does not match.";
                    return View(AdminAction);
                }
                PasswordChanger pc = new PasswordChanger();
                pc.CustomerId = AdminAction.CustomerId;
                pc.NewPassword = AdminAction.NewPassword;
                
                Task<Boolean> result = ResetPassword(pc);
                Boolean success = result.Result; 
                return View(AdminAction);
            } 
            else if(Action == "ResetCustomerId")
            {
                if (AdminAction.CustomerId == null)
                {
                    ViewData["Message"] = "you must enter a customer id.";
                    return View(AdminAction);
                }
                if (AdminAction.NewCustomerId == null)
                {
                    ViewData["Message"] = "you must enter a new customer id";
                    return View(AdminAction);
                }
                if (AdminAction.NewCustomerId2 == null)
                {
                    ViewData["Message"] = "you must enter a confirmation customer id";
                    return View(AdminAction);
                }
                if (AdminAction.NewCustomerId != AdminAction.NewCustomerId2)
                {
                    ViewData["Message"] = "Confirmation customer id does not match.";
                    return View(AdminAction);
                }
                if( AdminAction.CustomerId == AdminAction.NewCustomerId)
                {
                    ViewData["Message"] = "Customer Id and new Customer Id must be different.";
                    return View(AdminAction);
                }
                CustomerResetter cr = new CustomerResetter();
                cr.CustomerId = AdminAction.CustomerId;
                cr.NewCustomerId = AdminAction.NewCustomerId;

                var taskResult = ResetCustomerId(cr).Result;
                
                var msg = "";
                if(taskResult.Status == true)
                {
                    msg = "Customer successfully reset.";
                }
                else
                {
                    msg = taskResult.Message;
                }
                ViewData["message"] = msg; 
                return View(AdminAction);
            } 
            else if(Action == "ListCustomers")
            { 

                return RedirectToAction("CustomerList");
            }
            else
            {
                ViewData["Message"] = "unknown action.";
            }
            return View(AdminAction); 
        }


        private async Task<Boolean> ResetPassword(PasswordChanger PasswordChanger)
        {

            // PUT controller: route: api/Customers/   

            var goodResult = true;
            var uri = _send + "api/ChangePassword/";
            var client = _factory.CreateClient("ResetPassword");
            string json = JsonConvert.SerializeObject(PasswordChanger);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            //TODO: not: not add cors since in same project. 
            _logger.LogInformation("prepare reset password.");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("put exception" + m1);
                goodResult = false;
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString()); 
                ViewData["Message"] = ex.Message.ToString();
                goodResult = false;
            }
            var serverDown = (response == null);
            if (serverDown)
            {
                ViewData["Message"] = "Server Down - Contact Administration";
                return false;
            }
            // check 500,404
            var code = response.StatusCode.ToString();
            if (code == "BadRequest" || code == "NotFound" | code == "InternalServerError")
            {
                _logger.LogInformation("bad status:" + code);
                ViewData["Message"] = code;
                goodResult = false;
                return goodResult;
            }

            ViewData["Message"] = "Password reset successfully";
            return goodResult;
        }


        async Task<(Boolean Status, string Message)> ResetCustomerId(CustomerResetter CustomerResetter)
        {

            // PUT controller: route: api/ResetCustomer/   
            var goodResult = true;
            var msg = "";
            var uri = _send + "api/ResetCustomer/";
            var client = _factory.CreateClient("ResetCustomerId");
            string json = JsonConvert.SerializeObject(CustomerResetter);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Content = content,
                Method = HttpMethod.Put
            };

            var token = TempData.Peek("Token").ToString();
            request.Headers.Add("A65TOKEN", token);

            //TODO: not: not add cors since in same project. 
            _logger.LogInformation("prepare reset customer.");
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (HttpRequestException re)
            {
                msg = re.InnerException.Message.ToString(); 
                _logger.LogInformation("put exception" + msg);
                goodResult = false;
            }
            catch (System.Exception ex)
            {
                msg = ex.Message.ToString();
                _logger.LogInformation("put2 exception" + msg);
                goodResult = false;
            }
            var serverDown = (response == null);
            if(serverDown)
            {
                return (false, "Server Down - Contact Administration");
            }
            if(!goodResult)
            {

                return (false, "Reset Failed : status "
                    + response.StatusCode.ToString() + ":" +
                    msg);

            }
            // check 500,404 
            var code = response.StatusCode.ToString();
            if (code != "OK")
            {

                msg = "bad status code returned";
                return (false, "Reset Failed : status code "
                    + response.StatusCode.ToString() + ":" +
                    msg);
            }

            // app responses could be OK, or dup/missing message.

            HttpContent con = response.Content; 

            string appResult = await con.ReadAsStringAsync();   
            if(appResult == "OK")
            {
                return (true, "Good Result");
            }
            else
            {
                // appResult has dup or missing from id message...
                return (false, appResult);
            } 
        }

        public async Task<ActionResult<IEnumerable<Customer>>> CustomerList()  
        {

            // check for valid login
            var a = TempData["AdminSignedIn"];
            TempData.Keep();
            if (a == null)
            {
                return RedirectToAction("Index");
            }

            // GET controller: route: api/Customer/   

            var goodResult = true;
            var uri = _send + "api/Customer/";
            var client = _factory.CreateClient("ListCustomers"); 

            //TODO: not: not add cors since in same project. 
            _logger.LogInformation("prepare list customers.");
            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(uri);
            }
            catch (HttpRequestException re)
            {
                var m1 = re.InnerException.Message.ToString();
                ViewData["Message"] = m1;
                _logger.LogInformation("get exception" + m1);
                goodResult = false;
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("caught:" + ex.Message.ToString());
                ViewData["Message"] = ex.Message.ToString();
                goodResult = false;
            }
            // check 500,404  
             if (response == null) // server down - check null 
            {
                _logger.LogInformation("Server is down. Start it.");
                ViewData["Message"] = "Server is down. Start it.";
                goodResult = false;
                return View();
            }
            var code = response.StatusCode.ToString();
            if (code == "BadRequest" || code == "NotFound" | code == "ServerError")
            {
                _logger.LogInformation("bad status:" + code);
                ViewData["Message"] = code;
                goodResult = false; 
            }
            if (!goodResult)
            {
                _logger.LogInformation("error listing customers.");
            }
            else
            {
                _logger.LogInformation("list customers completed");
            }

            var content = response.Content;
            string json = await content.ReadAsStringAsync();
            var resultData = JsonConvert.DeserializeObject<List<Customer>>(json);

            CustomerList customerList = new CustomerList();

            foreach (var item in resultData)
            {
                Customer c = (Customer)item;
                customerList.customers.Add(c); // new model entry see claim hist
            } 
            return View(customerList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        protected String FormatScreenDate(string dbDate)
        {
            //  1/1/2020 blank
            var blankPosition = dbDate.IndexOf(" ");
            var output = dbDate.Substring(0, blankPosition);
            if(output == "1/1/1753")

            {
                output = "";
            }
            return output; 
        } 

    }
}
