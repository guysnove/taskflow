using Microsoft.EntityFrameworkCore;
using TaskFlow.Common;
using TaskFlow.Models;

namespace TaskFlow.Data;

// Brings the database up to date, and (in Development) adds sample projects and tasks if it's empty.
public static class SampleData
{
    public static async Task PrepareAsync(TaskFlowDbContext db, TimeProvider time, bool addSamples = true)
    {
        await db.Database.MigrateAsync();          // runs any migrations not applied yet

        if (!addSamples || await db.Tasks.AnyAsync())
        {
            return;                                 // production, or already has tasks: leave them alone
        }

        var today = time.Today();
        var office = new Project { Name = "Office move", Description = "Moving to the new building" };
        var report = new Project { Name = "Quarterly report" };
        db.Projects.AddRange(office, report);                 // Chapter 24: two sample projects
        db.Tasks.AddRange(
            new TaskItem
            {
                Title = "Write report", Priority = "High", Due = today.AddDays(-2),
                CreatedOn = today, Project = report
            },
            new TaskItem
            {
                Title = "Call supplier", Priority = "High", Due = today.AddDays(1),
                CreatedOn = today, Project = office
            },
            new TaskItem
            {
                Title = "Renew licence", Priority = "Medium", Due = today.AddDays(5),
                CreatedOn = today
            },
            new TaskItem { Title = "Plan team lunch", Priority = "Low", CreatedOn = today });
        await db.SaveChangesAsync();
    }
}
