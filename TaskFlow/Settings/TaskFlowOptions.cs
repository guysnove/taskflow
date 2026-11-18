using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Settings;

// TaskFlow's own settings, read from the "TaskFlow" section of the configuration
// (appsettings.json, environment variables, the command line…). See Program.cs.
public class TaskFlowOptions
{
    public const string Section = "TaskFlow";

    // The name shown in the menu and the browser tab.
    [Required, StringLength(40, MinimumLength = 1)]
    public string SiteName { get; set; } = "TaskFlow";

    // Tasks due within this many days count as "due soon" in the menu.
    [Range(0, 30)]
    public int DueSoonDays { get; set; } = 2;
}
