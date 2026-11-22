using System.ComponentModel.DataAnnotations;
using TaskFlow.Common;

namespace TaskFlow.Models;

// What the project forms send (Chapter 24).
public class ProjectInput
{
    [Required(ErrorMessage = "Please give the project a name.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "A name needs 2 to 60 characters.")]
    [NoControlCharacters(ErrorMessage = "A name can't contain line breaks or other control characters.")]   // Course 2, Ch. 20 (F1)
    public string? Name { get; set; }

    [Display(Name = "Description (optional)")]
    [StringLength(300, ErrorMessage = "A description can be at most 300 characters.")]
    [NoControlCharacters(AllowLineBreaks = true, ErrorMessage = "The description can't contain control characters.")]
    public string? Description { get; set; }

    public static ProjectInput From(Project project) => new()
    {
        Name = project.Name,
        Description = project.Description,
    };
}

// One row of the projects list: computed by the database, not loaded task by task.
public record ProjectSummary(int Id, string Name, string? Description, int Tasks, int Open);
