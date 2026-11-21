using TaskFlow.Models;

namespace TaskFlow.Services;

// Course 2, Chapter 9: Extract Class. What the task list should show, worked out from the
// address (/tasks?show=open&sort=priority&project=2&q=report). It used to be written inside
// TasksController.Index; now it can be tested without a web request.
public sealed record TaskListQuery(string Show, string Sort, int? Project, string? Q)
{
    // Unknown values fall back to the defaults: all tasks, by due date (FR-LIST-1).
    public static TaskListQuery From(string? show, string? sort, int? project, string? q) => new(
        Show: show == "open" ? "open" : "all",
        Sort: sort is "priority" or "title" ? sort : "due",
        Project: project,
        Q: string.IsNullOrWhiteSpace(q) ? null : q.Trim());

    // Course 2, Chapter 19: `today`, because the priority sort uses PriorityRules (FR-PRIO-1).
    public List<TaskItem> Apply(List<TaskItem> tasks, DateOnly today)
    {
        IEnumerable<TaskItem> shown = TaskSearch.Filter(tasks, Q);
        if (Project is not null)
        {
            shown = shown.Where(t => t.ProjectId == Project);
        }
        if (Show == "open")
        {
            shown = shown.Where(t => !t.Done);
        }
        return Sort switch
        {
            "priority" => shown.OrderBy(t => t.Done).ThenBy(t => PriorityRank(PriorityRules.For(t, today).Level)).ToList(),
            "title" => shown.OrderBy(t => t.Done).ThenBy(t => t.Title).ToList(),
            _ => shown.ToList(),                     // "due": the repository's order already
        };
    }

    // High first, then Medium, then Low.
    public static int PriorityRank(string priority) => priority switch
    {
        "High" => 0,
        "Medium" => 1,
        _ => 2,
    };
}
