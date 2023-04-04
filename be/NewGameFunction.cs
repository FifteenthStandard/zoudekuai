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
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        Console.WriteLine($"Request to create a new game");

        var host = await GetPlayerFromRequest(req, table);

        var gameCode = Guid.NewGuid().ToString().Substring(0, 4);

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
            Console.WriteLine($"Request to create a new game with Game Code {gameCode} by {host.Uuid} is processing");
            await table.AddEntityAsync(gameEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to create new game with Game Code {gameCode} by {host.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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

        Console.WriteLine($"Request to create new game with Game Code {gameCode} by {host.Uuid} was successful");

        var newGameResponse = new NewGameResponse
        {
            GameCode = gameCode,
        };

        return Ok(newGameResponse);
    }
}
