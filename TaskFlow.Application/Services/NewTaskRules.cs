using TaskFlow.Common;
using TaskFlow.Models;

namespace TaskFlow.Services;

// Course 2, Chapter 8: the rules for a NEW task, one class per rule (Single Responsibility),
// all behind one small interface. A new rule is a new class plus one line in Program.cs;
// NewTaskChecker and TasksController don't change (Open/Closed).

// Field: which form field the message belongs to. Warning: the user may go ahead anyway.
public sealed record RuleProblem(string Field, string Message, bool Warning = false);

public interface INewTaskRule
{
    bool IsWarning { get; }                          // warnings run only when there are no errors
    Task<RuleProblem?> CheckAsync(TaskInput input);  // null: this rule is happy
}

// FR-TASK-2: a new task can't be due in the past.
public class DueNotInPastRule(TimeProvider time) : INewTaskRule
{
    public bool IsWarning => false;

    public Task<RuleProblem?> CheckAsync(TaskInput input) =>
        Task.FromResult(input.Due < time.Today()
            ? new RuleProblem(nameof(input.Due), "A new task can't be due in the past.")
            : null);
}

// The chosen project must exist (and be yours: FindAsync only sees your projects).
public class ProjectExistsRule(IProjectRepository projects) : INewTaskRule
{
    public bool IsWarning => false;

    public async Task<RuleProblem?> CheckAsync(TaskInput input) =>
        input.ProjectId is int id && await projects.FindAsync(id) is null
            ? new RuleProblem(nameof(input.ProjectId), "Please choose a project from the list.")
            : null;
}

// FR-DUP-1 (Course 2, Chapter 2): warn about a probable duplicate, unless the user confirmed it.
public class DuplicateTitleRule(ITaskRepository tasks) : INewTaskRule
{
    public bool IsWarning => true;

    public async Task<RuleProblem?> CheckAsync(TaskInput input)
    {
        if (input.AllowDuplicate)
        {
            return null;
        }
        var existing = TitleMatcher.FindOpenDuplicate(input.Title, await tasks.GetAllAsync());
        return existing is null
            ? null
            : new RuleProblem(nameof(input.Title),
                $"You already have an open task called \"{existing.Title}\". Tick \"Add it anyway\" to add another.",
                Warning: true);
    }
}

// Runs the rules: first every error rule; the warnings only if nothing else is wrong.
// alreadyInvalid: the form's own checks (title length, priority) failed already.
public class NewTaskChecker(IEnumerable<INewTaskRule> rules)
{
    private readonly List<INewTaskRule> _rules = rules.ToList();

    public async Task<List<RuleProblem>> CheckAsync(TaskInput input, bool alreadyInvalid)
    {
        var problems = new List<RuleProblem>();
        foreach (var rule in _rules.Where(r => !r.IsWarning))
        {
            if (await rule.CheckAsync(input) is RuleProblem p)
            {
                problems.Add(p);
            }
        }
        if (problems.Count > 0 || alreadyInvalid)
        {
            return problems;
        }
        foreach (var rule in _rules.Where(r => r.IsWarning))
        {
            if (await rule.CheckAsync(input) is RuleProblem p)
            {
                problems.Add(p);
            }
        }
        return problems;
    }
}

// FR-TASK-3 (added in Chapter 8 to show Open/Closed): a due date more than five years ahead
// is almost always a typo (2062 instead of 2026), so it's refused.
public class DueWithinFiveYearsRule(TimeProvider time) : INewTaskRule
{
    public bool IsWarning => false;

    public Task<RuleProblem?> CheckAsync(TaskInput input) =>
        Task.FromResult(input.Due > time.Today().AddYears(5)
            ? new RuleProblem(nameof(input.Due), $"That's more than five years away: is {input.Due:yyyy} a typo?")
            : null);
}
