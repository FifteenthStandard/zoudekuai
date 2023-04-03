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

public class StartGameFunction : FunctionBase
{
    public class StartGameRequest
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
    }

    [FunctionName("start-game")]
    public async Task<IActionResult> StartGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        Console.WriteLine($"Request to start a game");

        var player = await GetPlayerFromRequest(req, table);

        var startGameRequest = await JsonSerializer.DeserializeAsync<StartGameRequest>(req.Body);
        var gameCode = startGameRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} is processing");
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} failed: Game not found");
                return BadRequest("Invalid gameCode");
            }
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        if (player.Uuid != gameEntity.HostUuid)
        {
            Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} failed: Not the host");
            return BadRequest("Not the host");
        }

        if (gameEntity.PlayerUuids.Count < 4)
        {
            Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} failed: Not enough players");
            return BadRequest("Not enough players");
        }

        if (gameEntity.Status != GameStatus.NotStarted)
        {
            Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} failed: Game already started");
            return BadRequest("Game already started");
        }

        gameEntity.Status = GameStatus.Started;
        gameEntity.RoundNumber = 1;

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
            TurnIndex = handEntities.FindIndex(hand => hand.Turn),
            StoleIndex = -1,
            PlayerUuids = gameEntity.PlayerUuids,
            PlayerNames = gameEntity.PlayerNames,
            PlayerCards = handEntities.Select(hand => hand.Cards.Count).ToList(),
        };

        try
        {
            Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} is processing");
            foreach (var handEntity in handEntities)
            {
                await table.AddEntityAsync(handEntity);
            }
            await table.AddEntityAsync(roundEntity);
            await table.UpsertEntityAsync(gameEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        foreach (var hand in handEntities)
        {
            var handMessage = new HandMessage
            {
                Turn = hand.Turn,
                Cards = hand.Cards
                    .Select(card =>
                        new HandMessage.Card
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
            Status = roundEntity.Status,
            RoundNumber = roundEntity.RoundNumber,
            Players = roundEntity.PlayerNames.Zip(roundEntity.PlayerCards)
                .Select((player, index) =>
                    new RoundMessage.Player
                    {
                        Name = player.First,
                        Cards = player.Second,
                        Turn = roundEntity.TurnIndex == index,
                        Stole = roundEntity.StoleIndex == index,
                    })
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
            Host = gameEntity.HostName,
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

        Console.WriteLine($"Request to start a game with Game Code {gameCode} by {player.Uuid} was successful");

        return Accepted();
    }
}