using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsLaser.Crm.Api.Infrastructure.Services;

namespace TsLaser.Crm.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class PagesController(TemplateService templateService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/")]
    public async Task<IActionResult> Home(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var html = await templateService.LoadAsync("index.html", cancellationToken: cancellationToken);
            return Content(html, "text/html; charset=utf-8");
        }

        var loginHtml = await templateService.LoadAsync("login.html", cancellationToken: cancellationToken);
        return Content(loginHtml, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        var template = User.Identity?.IsAuthenticated == true ? "index.html" : "login.html";
        var html = await templateService.LoadAsync(template, cancellationToken: cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("booking")]
    [HttpGet("book")]
    public async Task<IActionResult> Booking(CancellationToken cancellationToken)
    {
        var html = await templateService.LoadAsync("landing.html", cancellationToken: cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("clients/{clientId:int}/sessions")]
    public async Task<IActionResult> SessionsPage(int clientId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var loginHtml = await templateService.LoadAsync("login.html", cancellationToken: cancellationToken);
            return Content(loginHtml, "text/html; charset=utf-8");
        }

        var html = await templateService.LoadAsync(
            "sessions.html",
            new Dictionary<string, string>
            {
                ["{{ client_id }}"] = clientId.ToString(),
            },
            cancellationToken);

        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous]
    [HttpGet("bookings")]
    public async Task<IActionResult> BookingsPage(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var loginHtml = await templateService.LoadAsync("login.html", cancellationToken: cancellationToken);
            return Content(loginHtml, "text/html; charset=utf-8");
        }

        var html = await templateService.LoadAsync("bookings.html", cancellationToken: cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }
}
