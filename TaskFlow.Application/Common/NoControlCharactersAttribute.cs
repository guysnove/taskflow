using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Common;

// Course 2, Chapter 20 (security review, finding F1): one-line texts (titles, names) may not contain
// control characters: line breaks, tabs, bells, escape codes. A line break in a title can forge
// log lines and break email subjects. AllowLineBreaks: for multi-line texts like notes, where
// \n and \r are fine but the other control characters are not.
[AttributeUsage(AttributeTargets.Property)]
public sealed class NoControlCharactersAttribute : ValidationAttribute
{
    public bool AllowLineBreaks { get; init; }

    public NoControlCharactersAttribute() : base("{0} can't contain line breaks or other control characters.") { }

    public override bool IsValid(object? value) =>
        value is not string text || !text.Any(c => char.IsControl(c) && !(AllowLineBreaks && c is '\n' or '\r'));
}
