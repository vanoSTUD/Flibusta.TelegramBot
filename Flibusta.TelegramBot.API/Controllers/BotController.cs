using Flibusta.TelegramBot.Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace Flibusta.TelegramBot.API.Controllers;

[ApiController]
[Route("")]
public class BotController : ControllerBase
{
    private readonly IOptions<BotOptions> _options;

    public BotController(IOptions<BotOptions> options)
    {
        _options = options;
    }

    [HttpPost]
    public async Task<IActionResult> HandleUpdate([FromBody] Update update, [FromServices] UpdateHandler updateHandler, CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != _options.Value.SecretToken)
            return Forbid();

        try
        {
            await updateHandler.HandleUpdateAsync(update, ct);
        }
        catch (Exception exception)
        {
            updateHandler.HandleError(exception, ct);
        }

        return Ok();
    }
}