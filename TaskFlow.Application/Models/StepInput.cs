using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// What the "add a step" form (and the API) sends: only the text. (Chapter 30)
public class StepInput
{
    [Required(ErrorMessage = "Please write what the step is.")]
    [StringLength(120, ErrorMessage = "A step can be at most 120 characters.")]
    [Display(Name = "New step")]
    public string? Text { get; set; }
}
