using Microsoft.AspNetCore.Mvc;
using ServicePractice.Services;

namespace ServicePractice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscordController : ControllerBase
{
    private readonly DiscordWebhookService _discordWebhookService;
    private readonly IConfiguration _configuration;

    public DiscordController(
        DiscordWebhookService discordWebhookService,
        IConfiguration configuration)
    {
        _discordWebhookService = discordWebhookService;
        _configuration = configuration;
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestMessage()
    {
        string webhookUrl =
            _configuration["Discord:WebhookUrl"]
            ?? throw new InvalidOperationException(
                "Discord webhook is not configured"
            );

        bool success =
            await _discordWebhookService.SendMessageAsync(
                webhookUrl,
                "Test message from PUBG Result Notifier"
            );

        if (!success)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                "Discord send failed"
            );
        }

        return Ok("Discord message sent");
    }
}