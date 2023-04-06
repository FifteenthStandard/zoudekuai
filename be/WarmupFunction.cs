using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

public class WarmupFunction : FunctionBase
{
    [FunctionName("warmup")]
    public IActionResult Warmup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        logger.LogInformation("Warmup");

        return Ok();
    }
}
