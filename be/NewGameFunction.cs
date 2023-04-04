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

        logger.LogInformation("Request to create a new game");

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
        };

        try
        {
            logger.LogInformation("Request to create a new game with Game Code {gameCode} by {uuid} is processing", gameCode, host.Uuid);
            await table.AddEntityAsync(gameEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to create new game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, host.Uuid, ex.Message);
            throw;
        }

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = gameCode,
            ConnectionId = host.ConnectionId,
        });

        var gameMessage = new GameMessage
        {
            GameCode = gameEntity.GameCode,
            Status = gameEntity.Status,
            RoundNumber = gameEntity.RoundNumber,
            Players = gameEntity.PlayerNames,
        };

        await messages.AddAsync(
            new SignalRMessage
            {
                Target = "gameUpdate",
                GroupName = gameCode,
                Arguments = new [] { JsonSerializer.Serialize(gameMessage) },
            });

        logger.LogInformation("Request to create new game with Game Code {gameCode} by {uuid} was successful", gameCode, host.Uuid);

        var newGameResponse = new NewGameResponse
        {
            GameCode = gameCode,
        };

        return Ok(newGameResponse);
    }
}
