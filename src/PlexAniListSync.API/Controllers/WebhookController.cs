using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Plex;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.Webhook;

namespace PlexAniListSync.API.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IOptions<PlexOptions> _options;
    private readonly IOptions<AniListOptions> _aniOptions;
    private readonly IWebhookService _webhookService;

    public WebhookController(
        IOptions<PlexOptions> options,
        IOptions<AniListOptions> aniOptions,
        IWebhookService webhookService)
    {
        _options = options;
        _aniOptions = aniOptions;
        _webhookService = webhookService;
    }

    [HttpPost(Name = "Webhook")]
    public async Task<IActionResult> Post([FromBody] WebhookData data)
    {
        var result = await _webhookService.Handle(data);
        return result ? Ok() : BadRequest("Could not find Anilist show");
    }

    [HttpGet]
    [Route("Config")]
    public IActionResult GetConfig()
    {
        return Ok(new { Plex = _options.Value, Ani = _aniOptions.Value });
    }
}
