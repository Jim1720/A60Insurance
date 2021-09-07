 

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace A60Insurance
{
    public class ClaimScreenBodyTagHelper : TagHelperComponent 
    {
        public override int Order => 2;

        public override async Task ProcessAsync(TagHelperContext context,
                                                TagHelperOutput output)
        {

            try
            {


                if (string.Equals(context.TagName, "body",
                                  StringComparison.OrdinalIgnoreCase))
                {
                    var script = await File.ReadAllTextAsync(
                        "wwwroot/htm/ClaimFields.htm");
                    output.PostContent.AppendHtml(script);
                    Console.Write("BodyTagHelper.cs: body helper loaded 1");

                    script = await File.ReadAllTextAsync(
                       "wwwroot/htm/PaymentAmount.htm");
                    output.PostContent.AppendHtml(script);
                    Console.Write("BodyTagHelper.cs: body helper loaded 2");

                    // other screens use thre GeneralStyle..TagHelper....
                    script = await File.ReadAllTextAsync(
                     "wwwroot/htm/ColorSetup.htm");
                    output.PostContent.AppendHtml(script);
                    Console.Write("BodyTagHelper.cs: body helper loaded 3");


                }
            }
            catch (System.Exception ex)
            {
                Console.Write("body tag helper error is :" + ex.Message.ToString());
            }
        }
    }
}
