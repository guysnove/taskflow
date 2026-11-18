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
        var open = (await _repository.GetAllAsync()).Where(t => !t.Done).ToList();
        int overdue = open.Count(t => t.IsOverdue(today));
        int dueSoon = open.Count(t => t.IsDueSoon(today, _options.DueSoonDays));   // Chapter 28: one rule, tested
        return View(new OverdueSummary(overdue, dueSoon, _options.DueSoonDays));
    }
}
