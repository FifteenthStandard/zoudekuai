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
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        var requestId = Guid.NewGuid().ToString();
        var repository = new Repository(table, messages, logger, requestId);

        logger.LogInformation("[{requestId}] Request to register", requestId);

        var registerRequest = await JsonSerializer.DeserializeAsync<RegisterRequest>(req.Body);

        var uuid = GetUuidFromRequest(req);

        var playerEntity = new PlayerEntity
        {
            Uuid = uuid,
            Name = registerRequest.Name,
            ConnectionId = registerRequest.ConnectionId,
        };

        await repository.SavePlayerAsync(playerEntity);

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = uuid,
            ConnectionId = registerRequest.ConnectionId,
        });

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        return Accepted();
    }

}
