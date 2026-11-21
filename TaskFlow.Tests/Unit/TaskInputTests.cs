using System.ComponentModel.DataAnnotations;
using TaskFlow.Models;

namespace TaskFlow.Tests.Unit;

// The validation rules on TaskInput, checked the same way ASP.NET Core checks them.
[Trait("Req", "FR-TASK-1")]
public class TaskInputTests
{
    private static List<string> Errors(TaskInput input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "").ToList();
    }

    // [Theory]: the same test, once for each [InlineData] line.
    [Theory]
    [InlineData("ab", false)]           // too short
    [InlineData("abc", true)]           // the shortest allowed
    [InlineData("Call the supplier about the delivery date", true)]
    public void Title_Length_IsChecked(string title, bool valid)
    {
        var errors = Errors(new TaskInput { Title = title, Priority = "Medium" });
        Assert.Equal(valid, errors.Count == 0);
    }

    [Fact]
    public void Title_81Characters_IsRejectedWithTheFriendlyMessage()
    {
        var errors = Errors(new TaskInput { Title = new string('x', 81), Priority = "Medium" });
        Assert.Contains("A title needs 3 to 80 characters.", errors);
    }

    [Theory]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void Priority_KnownValues_AreAccepted(string priority)
    {
        Assert.Empty(Errors(new TaskInput { Title = "Valid title", Priority = priority }));
    }

    [Fact]
    public void Priority_Urgent_IsRejected()
    {
        var errors = Errors(new TaskInput { Title = "Valid title", Priority = "Urgent" });
        Assert.Contains("Please choose High, Medium or Low.", errors);
    }

    // Course 2, Chapter 18: the regression test for the bug the workflow test found, at unit-test speed.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BlankNotes_AreNoNotes(string? notes) => Assert.Null(new TaskInput { Notes = notes }.Notes);
}
