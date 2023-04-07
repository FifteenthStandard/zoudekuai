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

        var requestId = Guid.NewGuid().ToString();
        var repository = new Repository(table, messages, logger, requestId);

        logger.LogInformation("[{requestId}] Request to join an existing game", requestId);

        var player = await GetPlayerFromRequest(req, table);

        var joinGameRequest = await JsonSerializer.DeserializeAsync<JoinGameRequest>(req.Body);
        var gameCode = joinGameRequest.GameCode;

        var gameEntity = await repository.GetGameAsync(gameCode);
        if (gameEntity == null) return BadRequest("Invalid game code");

        if (gameEntity.PlayerUuids.Count == 5) return BadRequest("Game is full");

        if (gameEntity.PlayerUuids.Any(uuid => uuid == player.Uuid))
        {
            logger.LogInformation("[{requestId}] Rejoining", requestId);

            var joinMessage = new JoinMessage
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
                    Target = "gameJoin",
                    GroupName = player.Uuid,
                    Arguments = new [] { JsonSerializer.Serialize(joinMessage) },
                });

            if (gameEntity.Status == GameStatus.Started)
            {
                logger.LogInformation("[{requestId}] Game in progress, resending state messages", requestId);

                var roundNumber = gameEntity.RoundNumber;

                var roundEntity = await repository.GetCurrentRoundAsync(gameEntity);
                if (roundEntity == null) return BadRequest(); // TODO Too late

                var handEntity = await repository.GetHandAsync(gameEntity, player.Uuid);
                if (handEntity == null) return BadRequest(); // TODO Too late

                var roundMessage = new RoundMessage
                {
                    RoundNumber = roundEntity.RoundNumber,
                    Status = roundEntity.Status,
                    FreePlay = roundEntity.FreePlay,
                    Players = roundEntity.PlayerNames.Zip(roundEntity.PlayerCards)
                        .Select((player, index) =>
                            new RoundMessage.Player
                            {
                                Name = player.First,
                                Cards = player.Second,
                                Turn = roundEntity.TurnIndex == index,
                                Stole = roundEntity.StoleIndex == index,
                                Position = roundEntity.Positions.Contains(index)
                                    ? roundEntity.Positions.IndexOf(index)
                                    : null,
                            })
                        .ToList(),
                    Discard = roundEntity.Discard
                        .Select(cards => cards
                            .Select(card =>
                                new CardMessage
                                {
                                    Suit = card.Suit,
                                    Rank = card.Rank,
                                    Value = card.Value,
                                })
                            .ToList())
                        .ToList(),
                };

                await messages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "roundUpdate",
                        GroupName = player.Uuid,
                        Arguments = new [] { JsonSerializer.Serialize(roundMessage) },
                    });

                var handMessage = new HandMessage
                {
                    Turn = handEntity.Turn,
                    Cards = handEntity.Cards
                        .Select(card =>
                            new CardMessage
                            {
                                Suit = card.Suit,
                                Rank = card.Rank,
                                Value = card.Value,
                            })
                        .ToList(),
                };

                await messages.AddAsync(
                    new SignalRMessage
                    {
                        Target = "handUpdate",
                        GroupName = player.Uuid,
                        Arguments = new [] { JsonSerializer.Serialize(handMessage) },
                    });
            }
        }
        else
        {
            logger.LogInformation("[{requestId}] Adding to game", requestId);

            gameEntity.PlayerUuids.Add(player.Uuid);
            gameEntity.PlayerNames.Add(player.Name);

            var joinMessage = new JoinMessage
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
                    Target = "gameJoin",
                    GroupName = player.Uuid,
                    Arguments = new [] { JsonSerializer.Serialize(joinMessage) },
                });

            await repository.SaveGameAsync(gameEntity, "PlayerJoined", player.Name);
        }

        await actions.AddAsync(new SignalRGroupAction
        {
            Action = GroupAction.Add,
            GroupName = gameCode,
            ConnectionId = player.ConnectionId,
        });

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        return Accepted();
    }
}