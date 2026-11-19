using TaskFlow.Models;

namespace TaskFlow.Services;

// What TaskFlow needs from "somewhere that stores projects" (Chapter 24).
public interface IProjectRepository
{
    Task<List<ProjectSummary>> GetSummariesAsync();          // the list, with task counts
    Task<List<Project>> GetChoicesAsync();                   // for the task form's drop-down
    Task<Project?> FindAsync(int id);                        // just the project
    Task<Project?> FindWithTasksAsync(int id);               // the project and its tasks
    Task<bool> NameTakenAsync(string name, int? exceptId = null);
    Task<Project?> AddAsync(ProjectInput input);             // null: the name is taken
    Task<SaveResult> UpdateAsync(int id, ProjectInput input);
    Task<DeleteResult> DeleteAsync(int id);
}

public enum DeleteResult
{
    Deleted,
    NotFound,
    HasTasks,      // refused: the project still has tasks
}
