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
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        logger.LogInformation("Request to play cards");

        var player = await GetPlayerFromRequest(req, table);

        var playCardsRequest = await JsonSerializer.DeserializeAsync<PlayCardsRequest>(req.Body);
        var gameCode = playCardsRequest.GameCode;

        GameEntity gameEntity;
        try
        {
            logger.LogInformation("Request to retrieve a game with Game Code {gameCode} by {uuid} is processing", gameCode, player.Uuid);
            gameEntity = await table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                logger.LogWarning("Request to retrieve a game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, "Game not found");
                return BadRequest();
            }
            logger.LogInformation("Request to retrieve a game with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve a game with Game Code {gameCode} by {uuid} failed: {reason}", gameCode, player.Uuid, ex.Message);
            throw;
        }

        var roundNumber = gameEntity.RoundNumber;

        RoundEntity roundEntity;
        try
        {
            logger.LogInformation("Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} is processing", gameCode, roundNumber, player.Uuid);
            roundEntity = await table.GetEntityAsync<RoundEntity>(gameCode, roundNumber.ToString());
            if (roundEntity == null)
            {
                logger.LogWarning("Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, "Round not found");
                return BadRequest();
            }
            logger.LogInformation("Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} was successful", gameCode, roundNumber, player.Uuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, ex.Message);
            throw;
        }

        if (roundEntity.PlayerUuids[roundEntity.TurnIndex] != player.Uuid)
        {
            logger.LogWarning("Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, "Not your turn");
            return BadRequest("Not your turn");
        }

        HandEntity handEntity;
        try
        {
            logger.LogInformation("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {uuid} is processing", gameCode, roundNumber, player.Uuid);
            handEntity = await table.GetEntityAsync<HandEntity>(gameCode, $"{roundNumber}_{player.Uuid}");
            if (handEntity == null)
            {
                logger.LogWarning("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, "Hand not found");
                return BadRequest();
            }
            logger.LogInformation("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {uuid} was successful", gameCode, roundNumber, player.Uuid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, ex.Message);
            throw;
        }

        var playedCards = playCardsRequest.CardIndexes.OrderByDescending(i => i).Distinct().ToList();
        foreach (var cardIndex in playedCards)
        {
            if (cardIndex < 0 || cardIndex >= handEntity.Cards.Count)
            {
                logger.LogWarning("Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, "Invalid card index");
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
            logger.LogInformation("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {nextPlayer} is processing", gameCode, roundNumber, nextPlayer);
            nextHandEntity = await table.GetEntityAsync<HandEntity>(gameCode, $"{roundNumber}_{nextPlayer}");
            if (nextHandEntity == null)
            {
                logger.LogWarning("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} failed: {reason}", gameCode, roundNumber, nextPlayer, "Hand not found");
                return BadRequest();
            }
            logger.LogInformation("Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} was successful", gameCode, roundNumber, nextPlayer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} by {nextPlayer} failed: {reason}", gameCode, roundNumber, nextPlayer, ex.Message);
            throw;
        }

        nextHandEntity.Turn = true;

        try
        {
            logger.LogInformation("Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} is processing", gameCode, roundNumber, player.Uuid);
            await table.UpsertEntityAsync(handEntity);
            await table.UpsertEntityAsync(nextHandEntity);
            await table.UpsertEntityAsync(roundEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request to play cards in round with Game Code {gameCode} and Round Number {roundNumber} by {uuid} failed: {reason}", gameCode, roundNumber, player.Uuid, ex.Message);
            logger.LogError(ex.StackTrace);
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

        logger.LogInformation("Request to join an existing game with Game Code {gameCode} by {uuid} was successful", gameCode, player.Uuid);

        return Accepted();
    }
}