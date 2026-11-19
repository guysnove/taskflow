using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskFlow.Services;
using TaskFlow.Settings;

namespace TaskFlow.ViewComponents;

public record OverdueSummary(int Overdue, int DueSoon, int Days);

// A small piece of every page: how many tasks are overdue, and how many are due soon?
public class OverdueSummaryViewComponent : ViewComponent
{
    private readonly ITaskRepository _repository;
    private readonly TimeProvider _time;
    private readonly TaskFlowOptions _options;
    private readonly ICurrentUser _user;

    public OverdueSummaryViewComponent(ITaskRepository repository, TimeProvider time,
                                       IOptions<TaskFlowOptions> options, ICurrentUser user)
    {
        _repository = repository;
        _time = time;
        _options = options.Value;
        _user = user;
    }

    // Async since Chapter 23: the repository now reads from the database.
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_user.IsSignedIn)
        {
            return Content("");              // Chapter 25: nobody signed in, no tasks to count
        }
        var today = _time.Today();
        // Course 2, Chapter 5: this runs on EVERY page. It used to load every task (with projects
        // and steps) just to count two numbers: O(n) rows per page. Now the database counts.
        var counts = await _repository.CountDueAsync(today, _options.DueSoonDays);
        return View(new OverdueSummary(counts.Overdue, counts.DueSoon, _options.DueSoonDays));
    }
}
