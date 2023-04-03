using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

public class RegisterFunction : FunctionBase
{
    public class RegisterRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; }
    }

    [FunctionName("register")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRGroupAction> actions)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        var registerRequest = await JsonSerializer.DeserializeAsync<RegisterRequest>(req.Body);

        Console.WriteLine($"Request to register as {registerRequest.Name} connected via {registerRequest.ConnectionId}");

        var uuid = GetUuidFromRequest(req);

        var playerEntity = new PlayerEntity
        {
            Uuid = uuid,
            Name = registerRequest.Name,
            ConnectionId = registerRequest.ConnectionId,
        };

        try
        {
            Console.WriteLine($"Request to register as {registerRequest.Name} connected via {registerRequest.ConnectionId} by {uuid} is processing");
            await table.UpsertEntityAsync(playerEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to register as {registerRequest.Name} connected via {registerRequest.ConnectionId} by {uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = uuid,
            ConnectionId = registerRequest.ConnectionId,
        });

        Console.WriteLine($"Request to register as {registerRequest.Name} connected via {registerRequest.ConnectionId} by {uuid} was successful");

        return Accepted();
    }

}
