using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// What the add and edit forms send. Only the fields a visitor may change:
// no Id, no Done. (Why that matters: this chapter's case study.)
public class TaskInput
{
    [Required(ErrorMessage = "Please give the task a title.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "A title needs 3 to 80 characters.")]
    public string? Title { get; set; }

    [Required]
    [AllowedValues("High", "Medium", "Low", ErrorMessage = "Please choose High, Medium or Low.")]
    public string Priority { get; set; } = "Medium";

    [Display(Name = "Due date (optional)")]
    [DataType(DataType.Date)]            // makes asp-for write <input type="date">
    public DateOnly? Due { get; set; }

    [Display(Name = "Project (optional)")]
    public int? ProjectId { get; set; }

    // The Version the edit form was shown with (a hidden field). Not used by the add form.
    public int Version { get; set; }

    [Display(Name = "Notes (optional)")]
    [StringLength(500, ErrorMessage = "Notes can be at most 500 characters.")]
    public string? Notes
    {
        get;
        // Course 2, Chapter 18 (found by an integration test): the form sends "" as null, but the JSON API
        // kept "", so the same empty notes were stored two ways. Blank is now always "no notes".
        set => field = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // Course 2, Chapter 2: ticked when the user confirms they really want a second task
    // with the same title as an open one. Only the add form shows it.
    [Display(Name = "Add it anyway")]
    public bool AllowDuplicate { get; set; }

    // Builds a form model from an existing task, for the edit page.
    public static TaskInput From(TaskItem task) => new()
    {
        Title = task.Title,
        Priority = task.Priority,
        Due = task.Due,
        Notes = task.Notes,
        ProjectId = task.ProjectId,
        Version = task.Version,
    };
}
