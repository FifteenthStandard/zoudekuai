using System;
using System.Collections.Generic;
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

public class StartRoundFunction : FunctionBase
{
    public class StartRoundRequest
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
    }

    [FunctionName("start-round")]
    public async Task<IActionResult> StartRound(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        logger.LogInformation($"Request to start a round");

        var player = await GetPlayerFromRequest(req, table);

        var startRoundRequest = await JsonSerializer.DeserializeAsync<StartRoundRequest>(req.Body);
        var gameCode = startRoundRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            logger.LogInformation("Request to retrieve a game with Game Code {gameCode} by {uuid} is processing", gameCode, player.Uuid);
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                logger.LogWarning("Request to retrieve a game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, "Game not found");
                return BadRequest("Invalid gameCode");
            }
            logger.LogInformation("Request to retrieve a game with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve a game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, ex.Message);
            throw;
        }

        if (player.Uuid != gameEntity.HostUuid)
        {
            logger.LogWarning("Request to start a round with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, "Not the host");
            return BadRequest("Not the host");
        }

        if (gameEntity.PlayerUuids.Count < 4)
        {
            logger.LogWarning("Request to start a round with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, "Not enough players");
            return BadRequest("Not enough players");
        }

        // TODO check if round is already in progress

        gameEntity.Status = GameStatus.Started;
        gameEntity.RoundNumber += 1;

        var deck = new Deck(gameEntity.PlayerNames.Count);

        var handEntities = gameEntity.PlayerUuids.Zip(gameEntity.PlayerNames)
            .Select(player =>
            {
                var cards = deck.Deal();
                return new HandEntity
                {
                    GameCode = gameEntity.GameCode,
                    RoundNumber = gameEntity.RoundNumber,
                    Uuid = player.First,
                    Name = player.Second,
                    Turn = cards.First().Value == 0,
                    Cards = cards,
                };
            })
            .ToList();

        var roundEntity = new RoundEntity
        {
            GameCode = gameEntity.GameCode,
            RoundNumber = gameEntity.RoundNumber,
            Status = RoundStatus.Started,
            FreePlay = true,
            TurnIndex = handEntities.FindIndex(hand => hand.Turn),
            StoleIndex = -1,
            PlayerUuids = gameEntity.PlayerUuids,
            PlayerNames = gameEntity.PlayerNames,
            PlayerCards = handEntities.Select(hand => hand.Cards.Count).ToList(),
            Positions = new List<int>(),
        };

        try
        {
            logger.LogInformation("Request to start a round with Game Code {gameCode} by {uuid} is processing", gameCode, player.Uuid);
            foreach (var handEntity in handEntities)
            {
                await table.AddEntityAsync(handEntity);
            }
            await table.AddEntityAsync(roundEntity);
            await table.UpsertEntityAsync(gameEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to start a round with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, ex.Message);
            throw;
        }

        foreach (var hand in handEntities)
        {
            var handMessage = new HandMessage
            {
                Turn = hand.Turn,
                Cards = hand.Cards
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
                    GroupName = hand.Uuid,
                    Arguments = new [] { JsonSerializer.Serialize(handMessage) },
                });
        }

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
                GroupName = gameCode,
                Arguments = new [] { JsonSerializer.Serialize(roundMessage), "RoundStarted" },
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

        logger.LogInformation("Request to start a round with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);

        return Accepted();
    }
}