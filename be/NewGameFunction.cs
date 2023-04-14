using System;
using System.Linq;
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

public class NewGameFunction : FunctionBase
{
    public class NewGameResponse
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
    }

    [FunctionName("new-game")]
    public async Task<IActionResult> NewGame(
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

        logger.LogInformation("[{requestId}] Request to create a new game", requestId);

        var host = await GetPlayerFromRequest(req, table);

        var gameCode = new Random().Next(9999).ToString("D4");

        var gameEntity = new GameEntity
        {
            GameCode = gameCode,
            HostUuid = host.Uuid,
            HostName = host.Name,
            Status = GameStatus.NotStarted,
            RoundNumber = 0,
            PlayerUuids = { host.Uuid },
            PlayerNames = { host.Name },
            PlayerScores = { 40 },
        };

        var joinMessage = new JoinMessage
        {
            GameCode = gameEntity.GameCode,
            Status = gameEntity.Status,
            RoundNumber = gameEntity.RoundNumber,
            Players = gameEntity.PlayerNames,
            Host = true,
        };

        await messages.AddAsync(
            new SignalRMessage
            {
                Target = "gameJoin",
                GroupName = host.Uuid,
                Arguments = new [] { JsonSerializer.Serialize(joinMessage) },
            });

        await repository.SaveGameAsync(gameEntity);

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = gameCode,
            ConnectionId = host.ConnectionId,
        });

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        var newGameResponse = new NewGameResponse
        {
            GameCode = gameCode,
        };

        return Ok(newGameResponse);
    }
}
