using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

using TaskFlow.Services;

namespace TaskFlow.Data;

// TaskFlow's projects, kept in the database (Chapter 24). Since Chapter 25: only the user's own.
public class EfProjectRepository : IProjectRepository
{
    private readonly TaskFlowDbContext _db;
    private readonly ICurrentUser _user;

    public EfProjectRepository(TaskFlowDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    private IQueryable<Project> MyProjects()
    {
        int userId = _user.Id;
        return _db.Projects.Where(p => p.UserId == userId);
    }

    // One query: the counts are worked out by the database (a projection, Chapter 23).
    public Task<List<ProjectSummary>> GetSummariesAsync()
    {
        return MyProjects()
                  .OrderBy(p => p.Name)
                  .Select(p => new ProjectSummary(p.Id, p.Name, p.Description,
                                                  p.Tasks.Count(),
                                                  p.Tasks.Count(t => !t.Done)))
                  .ToListAsync();
    }

    public Task<List<Project>> GetChoicesAsync()
    {
        return MyProjects().AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Project?> FindAsync(int id)
    {
        return MyProjects().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    // The project and its tasks (unfinished first), in one query.
    public Task<Project?> FindWithTasksAsync(int id)
    {
        return MyProjects()
                  .AsNoTracking()
                  .Include(p => p.Tasks.OrderBy(t => t.Done).ThenBy(t => t.Title))
                  .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Is another project already using this name? (The column ignores capitals: NOCASE.)
    public Task<bool> NameTakenAsync(string name, int? exceptId = null)
    {
        string trimmed = name.Trim();
        return MyProjects().AnyAsync(p => p.Name == trimmed && p.Id != exceptId);
    }

    public async Task<Project?> AddAsync(ProjectInput input)
    {
        if (await NameTakenAsync(input.Name!))
        {
            return null;
        }
        var project = new Project
        {
            Name = input.Name!.Trim(),
            Description = input.Description,
            UserId = _user.Id,
        };
        _db.Projects.Add(project);
        try
        {
            await _db.SaveChangesAsync();
            return project;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return null;    // another request added the same name a moment ago
        }
    }

    public async Task<SaveResult> UpdateAsync(int id, ProjectInput input)
    {
        var project = await MyProjects().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return SaveResult.NotFound;
        }
        if (await NameTakenAsync(input.Name!, exceptId: id))
        {
            return SaveResult.NameTaken;
        }
        project.Name = input.Name!.Trim();
        project.Description = input.Description;
        try
        {
            await _db.SaveChangesAsync();
            return SaveResult.Saved;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return SaveResult.NameTaken;
        }
    }

    // A project that still has tasks is not deleted: the tasks would lose their project.
    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var project = await MyProjects().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return DeleteResult.NotFound;
        }
        if (await _db.Tasks.AnyAsync(t => t.ProjectId == id))
        {
            return DeleteResult.HasTasks;
        }
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    // SQLite error 19 is "constraint failed"; the message says which kind.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: 19 } sqlite
        && sqlite.Message.Contains("UNIQUE");
}
