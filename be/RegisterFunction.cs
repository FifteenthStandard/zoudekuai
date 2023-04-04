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
using Microsoft.Extensions.Logging;

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
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRGroupAction> actions,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        var registerRequest = await JsonSerializer.DeserializeAsync<RegisterRequest>(req.Body);

        logger.LogInformation("Request to register as {name} connected via {connectionId}", registerRequest.Name, registerRequest.ConnectionId);

        var uuid = GetUuidFromRequest(req);

        var playerEntity = new PlayerEntity
        {
            Uuid = uuid,
            Name = registerRequest.Name,
            ConnectionId = registerRequest.ConnectionId,
        };

        try
        {
            logger.LogInformation("Request to register as {name} connected via {connectionId} by {uuid} is processing", registerRequest.Name, registerRequest.ConnectionId, uuid);
            await table.UpsertEntityAsync(playerEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to register as {name} connected via {connectionId} by {uuid} failed: {reason}", registerRequest.Name, registerRequest.ConnectionId, uuid, ex.Message);
            throw;
        }

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = uuid,
            ConnectionId = registerRequest.ConnectionId,
        });

        logger.LogInformation("Request to register as {name} connected via {connectionId} by {uuid} was successful", registerRequest.Name, registerRequest.ConnectionId, uuid);

        return Accepted();
    }

}
