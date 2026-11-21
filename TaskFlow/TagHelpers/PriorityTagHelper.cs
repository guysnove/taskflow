using Microsoft.AspNetCore.Razor.TagHelpers;
using TaskFlow.Models;

namespace TaskFlow.TagHelpers;

// <priority value="High" />  becomes  <span class="priority priority-high">High</span>
// Course 2, Chapter 19: <priority value="Low" effective="@PriorityRules.For(task, today)" /> shows the
// EFFECTIVE priority, with an arrow and the reason when it was raised (FR-PRIO-1):
//   <span class="priority priority-medium priority-raised" title="Raised from Low: due today">Medium ↑ …</span>
public class PriorityTagHelper : TagHelper
{
    public string? Value { get; set; }
    public EffectivePriority? Effective { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        string chosen = Value ?? "Medium";
        string text = Effective?.Level ?? chosen;
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        if (Effective?.Raised is string why)
        {
            string reason = $"Raised from {chosen}: {why}";
            output.Attributes.SetAttribute("class", $"priority priority-{text.ToLowerInvariant()} priority-raised");
            output.Attributes.SetAttribute("title", reason);
            output.Content.SetContent(text + " ");
            output.Content.AppendHtml("<span aria-hidden=\"true\">&#x2191;</span>");
            output.Content.AppendHtml($"<span class=\"visually-hidden\">({System.Net.WebUtility.HtmlEncode(reason)})</span>");
            return;
        }
        output.Attributes.SetAttribute("class", $"priority priority-{text.ToLowerInvariant()}");
        output.Content.SetContent(text);
    }
}
