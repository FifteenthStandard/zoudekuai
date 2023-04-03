using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class FunctionBase
{
    protected IActionResult Accepted() => new AcceptedResult();
    protected IActionResult BadRequest() => new BadRequestResult();
    protected IActionResult BadRequest(string message) => new ObjectResult(message) { StatusCode = 400 };
    protected IActionResult Ok() => new OkResult();
    protected IActionResult Ok(object result) => new JsonResult(result) { StatusCode = 200 };

    protected void SetCorsHeaders(HttpRequest req)
    {
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:3000");
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization,x-requested-with,x-signalr-user-agent");
    }

    protected bool IsCorsPreflight(HttpRequest req) => req.Method.ToLower() == "options";

    protected string GetUuidFromRequest(HttpRequest req)
    {
        var auth = req.Headers["Authorization"].Single();
        if (!auth.StartsWith("Basic ")) throw new UnauthorizedAccessException();
        return auth.Substring("Basic ".Length);
    }

    protected async Task<PlayerEntity> GetPlayerFromRequest(HttpRequest req, TableClient table)
    {
        var uuid = GetUuidFromRequest(req);
        var player = await table.GetEntityAsync<PlayerEntity>(PlayerEntity.PlayerPartitionKey, uuid);
        if (player == null)
        {
            Console.WriteLine($"Player with UUID {uuid} not found");
            throw new UnauthorizedAccessException();
        }

        return player;
    }
}
