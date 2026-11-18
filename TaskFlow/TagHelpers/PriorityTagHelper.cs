using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskFlow.TagHelpers;

// <priority value="High" />  becomes  <span class="priority priority-high">High</span>
public class PriorityTagHelper : TagHelper
{
    public string? Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        string text = Value ?? "Medium";
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"priority priority-{text.ToLowerInvariant()}");
        output.Content.SetContent(text);
    }
}
