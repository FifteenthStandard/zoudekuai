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

public class PlayCardsFunction : FunctionBase
{
    public class PlayCardsRequest
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
        [JsonPropertyName("cardIndexes")]
        public List<int> CardIndexes { get; set; }
    }

    [FunctionName("play-cards")]
    public async Task<IActionResult> JoinGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRGroupAction> actions,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        Console.WriteLine($"Request to play cards");

        var player = await GetPlayerFromRequest(req, table);

        var playCardsRequest = await JsonSerializer.DeserializeAsync<PlayCardsRequest>(req.Body);
        var gameCode = playCardsRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} is processing");
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} failed: Game not found");
                return BadRequest();
            }
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve a game with Game Code {gameCode} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        var roundNumber = gameEntity.RoundNumber;

        RoundEntity roundEntity;
        try
        {
            Console.WriteLine($"Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} is processing");
            roundEntity = await table.GetEntityAsync<RoundEntity>(gameCode, roundNumber.ToString());
            if (roundEntity == null)
            {
                Console.WriteLine($"Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: Round not found");
                return BadRequest();
            }
            Console.WriteLine($"Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        if (roundEntity.PlayerUuids[roundEntity.TurnIndex] != player.Uuid)
        {
            Console.WriteLine($"Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: Not your turn");
            return BadRequest("Not your turn");
        }

        HandEntity handEntity;
        try
        {
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} is processing");
            handEntity = await table.GetEntityAsync<HandEntity>(gameCode, $"{roundNumber}_{player.Uuid}");
            if (handEntity == null)
            {
                Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: Hand not found");
                return BadRequest();
            }
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        var playedCards = playCardsRequest.CardIndexes.OrderByDescending(i => i).Distinct().ToList();
        foreach (var cardIndex in playedCards)
        {
            if (cardIndex < 0 || cardIndex >= handEntity.Cards.Count)
            {
                Console.WriteLine($"Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: Invalid card index");
                return BadRequest("Invalid card index");
            }

            // TODO legal move?

            handEntity.Cards.RemoveAt(cardIndex);
        }
        handEntity.Turn = false;

        roundEntity.PlayerCards[roundEntity.TurnIndex] -= playedCards.Count;
        roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count;

        var nextPlayer = roundEntity.PlayerUuids[roundEntity.TurnIndex];
        HandEntity nextHandEntity;
        try
        {
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {nextPlayer} is processing");
            nextHandEntity = await table.GetEntityAsync<HandEntity>(gameCode, $"{roundNumber}_{nextPlayer}");
            if (nextHandEntity == null)
            {
                Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} failed: Hand not found");
                return BadRequest();
            }
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} was successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        nextHandEntity.Turn = true;

        try
        {
            Console.WriteLine($"Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} is processing");
            await table.UpsertEntityAsync(handEntity);
            await table.UpsertEntityAsync(nextHandEntity);
            await table.UpsertEntityAsync(roundEntity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {player.Uuid} failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }

        var handMessage = new HandMessage
        {
            Turn = handEntity.Turn,
            Cards = handEntity.Cards
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
                GroupName = handEntity.Uuid,
                Arguments = new [] { JsonSerializer.Serialize(handMessage) },
            });

        var nextHandMessage = new HandMessage
        {
            Turn = nextHandEntity.Turn,
            Cards = nextHandEntity.Cards
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
                GroupName = nextHandEntity.Uuid,
                Arguments = new [] { JsonSerializer.Serialize(nextHandMessage) },
            });

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
                Arguments = new [] { JsonSerializer.Serialize(roundMessage), "CardsPlayed", player.Name },
            });

        Console.WriteLine($"Request to join an existing game with Game Code {gameCode} by {player.Uuid} was successful");

        return Accepted();
    }
}