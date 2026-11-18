using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskFlow.TagHelpers;

// <li show-when="signed-in">…</li> is only written if someone is signed in (Chapter 25).
// Values: "signed-in", "signed-out" or "admin". Otherwise the element disappears completely.
[HtmlTargetElement(Attributes = "show-when")]
public class ShowWhenTagHelper : TagHelper
{
    [HtmlAttributeName("show-when")]
    public string When { get; set; } = "";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = ViewContext.HttpContext.User;
        bool signedIn = user.Identity?.IsAuthenticated == true;
        bool show = When switch
        {
            "signed-in" => signedIn,
            "signed-out" => !signedIn,
            "admin" => signedIn && user.IsInRole("Admin"),
            _ => throw new InvalidOperationException($"show-when=\"{When}\" isn't a known value."),
        };
        if (!show)
        {
            output.SuppressOutput();       // write nothing: not the element, not its content
        }
    }
}
