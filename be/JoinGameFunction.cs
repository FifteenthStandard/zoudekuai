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

public class JoinGameFunction : FunctionBase
{
    public class JoinGameRequest
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
    }

    [FunctionName("join-game")]
    public async Task<IActionResult> JoinGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRGroupAction> actions,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        Console.WriteLine($"Request to join an existing game");

        var player = await GetPlayerFromRequest(req, table);

        var joinGameRequest = await JsonSerializer.DeserializeAsync<JoinGameRequest>(req.Body);
        var gameCode = joinGameRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            Console.WriteLine($"Request to retrieve an existing game with Game Code {gameCode} by {player.Uuid} is processing");
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                Console.WriteLine($"Request to retrieve an existing game with Game Code {gameCode} by {player.Uuid} failed: Game not found");
                return BadRequest();
            }
            Console.WriteLine($"Request to retrieve an existing game with Game Code {gameCode} by {player.Uuid} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve an existing game with Game Code {gameCode} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = gameCode,
            ConnectionId = player.ConnectionId,
        });

        if (gameEntity.PlayerUuids.Any(uuid => uuid == player.Uuid))
        {
            var gameMessage = new RejoinMessage
            {
                GameCode = gameEntity.GameCode,
                Status = gameEntity.Status,
                RoundNumber = gameEntity.RoundNumber,
                Players = gameEntity.PlayerNames,
                Host = player.Uuid == gameEntity.HostUuid,
            };

            await messages.AddAsync(
                new SignalRMessage
                {
                    Target = "rejoin",
                    GroupName = player.Uuid,
                    Arguments = new [] { JsonSerializer.Serialize(gameMessage), "Rejoined" },
                });
        }
        else
        {
            gameEntity.PlayerUuids.Add(player.Uuid);
            gameEntity.PlayerNames.Add(player.Name);
            try
            {
                Console.WriteLine($"Request to join an existing game with Game Code {gameCode} by {player.Uuid} is processing");
                await table.UpsertEntityAsync(gameEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request to join an existing game with Game Code {gameCode} by {player.Uuid} failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }

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
                    Arguments = new [] { JsonSerializer.Serialize(gameMessage), "PlayerJoined", player.Name },
                });
        }

        Console.WriteLine($"Request to join an existing game with Game Code {gameCode} by {player.Uuid} was successful");

        return Accepted();
    }
}