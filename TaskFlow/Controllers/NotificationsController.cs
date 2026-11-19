using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Notifications;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Course 2, Chapter 10: the signed-in user's notifications (FR-NOTIF-3) and email setting (FR-NOTIF-4).
[Route("notifications")]
public class NotificationsController(INotificationRepository notifications, ICurrentUser user, TaskFlowDbContext db) : Controller
{
    // GET /notifications
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await notifications.LatestAsync(user.Id, 50));

    // POST /notifications/read
    [HttpPost("read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await notifications.MarkAllReadAsync(user.Id);
        return RedirectToAction(nameof(Index));
    }

    // GET /notifications/settings
    [HttpGet("settings")]
    public async Task<IActionResult> Settings()
    {
        var me = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        return View(me.EmailNotifications);
    }

    // POST /notifications/settings   (NFR-USE-1: one tick and one click)
    [HttpPost("settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(bool emailNotifications)
    {
        await db.Users.Where(u => u.Id == user.Id)
                      .ExecuteUpdateAsync(set => set.SetProperty(u => u.EmailNotifications, emailNotifications));
        TempData["Message"] = emailNotifications ? "Notifications will also be emailed to you." : "No more notification emails.";
        return RedirectToAction(nameof(Index));
    }
}
