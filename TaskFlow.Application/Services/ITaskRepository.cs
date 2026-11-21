using TaskFlow.Models;

namespace TaskFlow.Services;

// What TaskFlow needs from "somewhere that stores tasks".
// Since Chapter 23 every method is asynchronous: a database answers over time,
// and while a request waits, the server can work on other requests.
public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskPage> GetPageAsync(TaskFilter filter, int page, int pageSize);   // Course 2, Chapter 15: the API's list
    Task<TaskItem?> FindAsync(int id);
    Task<TaskItem> AddAsync(TaskInput input);
    Task<SaveResult> UpdateAsync(int id, TaskInput input);    // Chapter 24: Saved, NotFound or Conflict
    Task<bool> MarkDoneAsync(int id);
    Task<bool> DeleteAsync(int id);                           // Chapter 24
    Task<DueCounts> CountDueAsync(DateOnly today, int days);  // Course 2, Chapter 5: counted by the database
}

// Course 2, Chapter 15: which tasks the API's list asks for (null = don't filter), and one page of them.
public record TaskFilter(bool? Done, int? ProjectId);
public record TaskPage(List<TaskItem> Items, int Total);

// How many open tasks are overdue, and how many are due in the next few days (Course 2, Chapter 5).
public record DueCounts(int Overdue, int DueSoon);

// The three ways an update can end (Chapter 24).
public enum SaveResult
{
    Saved,
    NotFound,
    Conflict,      // someone else saved the task after this form was shown
    NameTaken,     // projects only: another project already has this name
}
