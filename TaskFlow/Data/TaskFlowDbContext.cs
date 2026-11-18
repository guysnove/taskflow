using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Data;

// The bridge between TaskFlow's C# classes and the SQLite database.
public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options)
    {
    }

    // One DbSet per table: this one is the Tasks table.
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskStep> Steps => Set<TaskStep>();     // Chapter 30

    // The rules for the table: the same ones as Chapter 22's 01_schema.sql.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(task =>
        {
            task.ToTable("Tasks", table => table.HasCheckConstraint(
                "CK_Tasks_Priority", "Priority IN ('High', 'Medium', 'Low')"));
            task.Property(t => t.Title).IsRequired().HasMaxLength(80);
            task.Property(t => t.Priority).IsRequired().HasMaxLength(10).HasDefaultValue("Medium");
            task.Property(t => t.Notes).HasMaxLength(500);
            task.HasIndex(t => t.Due);
            task.Property(t => t.Version).IsConcurrencyToken();     // checked on UPDATE and DELETE

            // Each task may belong to one project; a project with tasks can't be deleted.
            task.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chapter 25: each task belongs to a user.
            task.HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).IsRequired().HasMaxLength(60)
                   .UseCollation("NOCASE");                         // "office move" = "Office Move"
            // Chapter 25: names are unique per user (two people may both have "Office move").
            project.HasIndex(p => new { p.UserId, p.Name }).IsUnique();
            project.HasOne(p => p.User)
                   .WithMany(u => u.Projects)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            project.Property(p => p.Description).HasMaxLength(300);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).IsRequired().HasMaxLength(254).UseCollation("NOCASE");
            user.HasIndex(u => u.Email).IsUnique();                  // one account per email address
            user.Property(u => u.DisplayName).IsRequired().HasMaxLength(60);
            user.Property(u => u.PasswordHash).IsRequired();
        });

        // Chapter 30: a task's checklist. Deleting a task deletes its steps (Cascade).
        modelBuilder.Entity<TaskStep>(step =>
        {
            step.Property(s => s.Text).IsRequired().HasMaxLength(120);
            step.HasOne(s => s.TaskItem)
                .WithMany(t => t.Steps)
                .HasForeignKey(s => s.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
            step.HasIndex(s => new { s.TaskItemId, s.Position });   // "this task's steps, in order"
        });
    }
}
