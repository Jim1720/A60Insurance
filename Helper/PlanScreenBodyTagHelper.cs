

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace A60Insurance
{
    public class PlanScreenBodyTagHelper : TagHelperComponent
    {
        public override int Order => 1;

        public override async Task ProcessAsync(TagHelperContext context,
                                                TagHelperOutput output)
        {

            try
            {


                if (string.Equals(context.TagName, "body",
                                  StringComparison.OrdinalIgnoreCase))
                {
                    

                    var script = await File.ReadAllTextAsync(
                     "wwwroot/htm/PlanSelect.htm");
                    output.PostContent.AppendHtml(script);
                  //  Console.Write("PlanScreenBodyTagHelper.cs: body helper loaded 3");
                }
            }
            catch (System.Exception ex)
            {
                Console.Write("plan screen body tag helper error is :" + ex.Message.ToString());
            }
        }
    }
}
