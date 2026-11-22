using TaskFlow.Common;

namespace TaskFlow.Tests.Unit;

// Course 2, Chapter 20: the rule behind finding F1, at unit-test speed.
public class NoControlCharactersTests
{
    [Theory]
    [InlineData("Pay rent", true)]
    [InlineData("Café, naïve, 日本語, emoji 🎉", true)]      // letters from every language are fine
    [InlineData(null, true)]                                  // [Required] decides about missing values
    [InlineData("Pay\nrent", false)]
    [InlineData("Pay\rrent", false)]
    [InlineData("Pay\trent", false)]
    [InlineData("Pay\u001b[31mrent", false)]                  // a terminal colour code
    public void OneLineText(string? text, bool valid) =>
        Assert.Equal(valid, new NoControlCharactersAttribute().IsValid(text));

    [Theory]
    [InlineData("Line one\nLine two", true)]
    [InlineData("Windows\r\nline", true)]
    [InlineData("A bell\u0007", false)]
    public void MultiLineText(string text, bool valid) =>
        Assert.Equal(valid, new NoControlCharactersAttribute { AllowLineBreaks = true }.IsValid(text));
}

// Course 2, Chapter 20: finding F2's helper.
public class LogSafeTests
{
    [Theory]
    [InlineData("ana.lopez@example.com", "a***@example.com")]
    [InlineData("x@y.org", "x***@y.org")]
    [InlineData("not-an-email", "***")]
    [InlineData(null, "(none)")]
    public void Email_KeepsOnlyTheFirstLetterAndTheDomain(string? address, string expected) =>
        Assert.Equal(expected, LogSafe.Email(address));
}
