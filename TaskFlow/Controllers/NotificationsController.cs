using Microsoft.AspNetCore.Mvc;
using TaskFlow.Notifications;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Course 2, Chapter 10: the signed-in user's notifications (FR-NOTIF-3) and email setting (FR-NOTIF-4).
[Route("notifications")]
public class NotificationsController(INotificationRepository notifications, ICurrentUser user, AccountService accounts) : Controller
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
        return View(await accounts.EmailNotificationsAsync(user.Id));
    }

    // POST /notifications/settings   (NFR-USE-1: one tick and one click)
    [HttpPost("settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(bool emailNotifications)
    {
        await accounts.SetEmailNotificationsAsync(user.Id, emailNotifications);
        TempData["Message"] = emailNotifications ? "Notifications will also be emailed to you." : "No more notification emails.";
        return RedirectToAction(nameof(Index));
    }
}
