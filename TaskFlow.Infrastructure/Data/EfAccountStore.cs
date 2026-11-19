using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Data;

// Course 2, Chapter 12: the EF Core ADAPTER for the IAccountStore port. The same queries that
// used to be inside AccountService, unchanged.
public class EfAccountStore(TaskFlowDbContext db) : IAccountStore
{
    public Task<bool> EmailTakenAsync(string email) => db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> AnyAsync() => db.Users.AnyAsync();

    public async Task<bool> AddAsync(User user)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<(int Tasks, int Projects)> AdoptOrphansAsync(int userId)
    {
        int tasks = await db.Tasks.Where(t => t.UserId == null)
                                  .ExecuteUpdateAsync(s => s.SetProperty(t => t.UserId, userId));
        int projects = await db.Projects.Where(p => p.UserId == null)
                                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, userId));
        return (tasks, projects);
    }

    public Task<User?> FindByEmailAsync(string email) => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task SaveAsync() => db.SaveChangesAsync();

    public Task<List<UserSummary>> SummariesAsync() =>
        db.Users.OrderBy(u => u.Id)
                .Select(u => new UserSummary(u.Id, u.Email, u.DisplayName, u.CreatedOn, u.IsAdmin, u.Tasks.Count()))
                .ToListAsync();

    public Task<bool> EmailNotificationsAsync(int userId) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.EmailNotifications).SingleAsync();

    public Task SetEmailNotificationsAsync(int userId, bool enabled) =>
        db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(set => set.SetProperty(u => u.EmailNotifications, enabled));
}
