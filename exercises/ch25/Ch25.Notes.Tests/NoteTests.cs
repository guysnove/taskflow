using Ch25.Notes;

public class NoteTests
{
    [Theory]
    [InlineData("Buy milk", null)]
    [InlineData("", "A note needs some text.")]
    [InlineData("   ", "A note needs some text.")]
    public void Validate(string text, string? expected) => Assert.Equal(expected, Note.Validate(text));

    [Fact]
    public void Validate_TooLong() =>
        Assert.Equal("A note can have at most 200 characters.", Note.Validate(new string('x', 201)));

    [Fact]
    public void Validate_TrimsBeforeCounting() => Assert.Null(Note.Validate(new string('x', 200) + "   "));
}
