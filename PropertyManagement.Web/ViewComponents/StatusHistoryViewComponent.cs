using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewComponents;

// Renders an application's status history (who, when, comment). Only
// manager pages invoke it; the controller has already authorized access.
public class StatusHistoryViewComponent(
    RentalApplicationService applicationService)
    : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var history = await applicationService.GetStatusHistoryAsync(
            applicationId);

        return View(history);
    }
}
