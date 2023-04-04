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
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        logger.LogInformation("Request to join an existing game");

        var player = await GetPlayerFromRequest(req, table);

        var joinGameRequest = await JsonSerializer.DeserializeAsync<JoinGameRequest>(req.Body);
        var gameCode = joinGameRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            logger.LogInformation("Request to retrieve an existing game with Game Code {gameCode} by {uuid} is processing", gameCode, player.Uuid);
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                logger.LogWarning("Request to retrieve an existing game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, "Game not found");
                return BadRequest();
            }
            logger.LogInformation("Request to retrieve an existing game with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve an existing game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, ex.Message);
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
                logger.LogInformation("Request to join an existing game with Game Code {gameCode} by {uuid} is processing", gameCode, player.Uuid);
                await table.UpsertEntityAsync(gameEntity);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Request to join an existing game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, ex.Message);
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

        logger.LogInformation("Request to join an existing game with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);

        return Accepted();
    }
}