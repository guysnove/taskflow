using Microsoft.AspNetCore.Mvc;
using TaskFlow.Notifications;
using TaskFlow.Services;

namespace TaskFlow.ViewComponents;

// Course 2, Chapter 10: "Notifications (2)" in the menu, on every page. One COUNT query.
public class UnreadNotificationsViewComponent(INotificationRepository notifications, ICurrentUser user) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        !user.IsSignedIn ? Content("") : View(await notifications.UnreadCountAsync(user.Id));
}
