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
    public async Task<IActionResult> PlayCards(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        var requestId = Guid.NewGuid().ToString();
        var repository = new Repository(table, messages, logger, requestId);

        logger.LogInformation("[{requestId}] Request to play cards", requestId);

        var player = await GetPlayerFromRequest(req, table);

        var playCardsRequest = await JsonSerializer.DeserializeAsync<PlayCardsRequest>(req.Body);
        var gameCode = playCardsRequest.GameCode;

        var gameEntity = await repository.GetGameAsync(gameCode);
        if (gameEntity == null) return BadRequest("Invalid game code");

        var roundNumber = gameEntity.RoundNumber;

        var roundEntity = await repository.GetCurrentRoundAsync(gameEntity);
        if (roundEntity == null) return BadRequest();

        if (roundEntity.PlayerUuids[roundEntity.TurnIndex] != player.Uuid)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Not your turn");
            return BadRequest("Not your turn");
        }

        var handEntity = await repository.GetHandAsync(gameEntity, player.Uuid);
        if (handEntity == null) return BadRequest();

        var playedCardIndexes = playCardsRequest.CardIndexes.OrderByDescending(i => i).Distinct().ToList();
        var playedCards = playedCardIndexes.Select(index => handEntity.Cards[index]).ToList();

        if (roundEntity.StealChances > 0)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Can't play cards during steal round");
            return BadRequest("Can't play cards during steal round");
        }

        if (!roundEntity.FreePlay && !roundEntity.FirstPlayContinuation && playedCards.Count != roundEntity.Discard.Last().Count)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Count must match current play");
            return BadRequest("Count must match current play");
        }

        if (!roundEntity.FreePlay && !roundEntity.FirstPlayContinuation && playedCards[0].Value < roundEntity.Discard.Last()[0].Value)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Cards must beat current play");
            return BadRequest("Cards must beat current play");
        }

        int? rank = roundEntity.FirstPlayContinuation ? 0 : null;
        foreach (var cardIndex in playedCardIndexes)
        {
            if (cardIndex < 0 || cardIndex >= handEntity.Cards.Count)
            {
                logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Invalid card index");
                return BadRequest("Invalid card index");
            }

            if (rank != null && handEntity.Cards[cardIndex].Rank != rank)
            {
                logger.LogWarning("Request failed: {reason}", requestId, "Cards don't match");
                return BadRequest("Cards don't match");
            }

            rank = handEntity.Cards[cardIndex].Rank;

            handEntity.Cards.RemoveAt(cardIndex);
        }
        handEntity.Turn = false;

        if (roundEntity.FirstPlayContinuation)
        {
            roundEntity.Discard[0] = playedCards.Concat(roundEntity.Discard[0]).ToList();
            roundEntity.PlayerCards[roundEntity.TurnIndex] -= playedCardIndexes.Count;
            playedCards.Add(Card.FromValue(0));
            roundEntity.FirstPlayContinuation = false;
        }
        else
        {
            roundEntity.Discard.Add(playedCards);
            roundEntity.PlayerCards[roundEntity.TurnIndex] -= playedCardIndexes.Count;
        }

        await repository.SaveHandAsync(handEntity);

        logger.LogInformation("[{requestId}] Cards played", requestId);

        if (!handEntity.Cards.Any())
        {
            logger.LogInformation("[{requestId}] Player out of cards", requestId);
            roundEntity.Positions.Add(roundEntity.TurnIndex);

            if (roundEntity.StoleIndex >= 0)
            {
                roundEntity.Status = RoundStatus.Finished;

                await repository.SaveRoundAsync(roundEntity);

                logger.LogInformation("[{requestId}] Completed successfully", requestId);

                return Accepted();
            }
        }

        if (roundEntity.Positions.Count == roundEntity.PlayerUuids.Count - 1)
        {
            roundEntity.Positions.Add(roundEntity.PlayerCards.FindIndex(cards => cards > 0));
            roundEntity.TurnIndex = -1;
            roundEntity.Status = RoundStatus.Finished;
        }
        else
        {
            var startingTurn = roundEntity.TurnIndex;
            HandEntity nextHandEntity = handEntity;
            bool playEnd = true;
            for (
                roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count;
                roundEntity.TurnIndex != startingTurn;
                roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count)
            {
                var nextPlayer = roundEntity.PlayerUuids[roundEntity.TurnIndex];

                if (roundEntity.PlayerCards[roundEntity.TurnIndex] == 0)
                {
                    logger.LogInformation("[{requestId}] Skipping {nextPlayer}: {reason}", requestId, nextPlayer, "No cards");
                    continue;
                }

                nextHandEntity = await repository.GetHandAsync(gameEntity, nextPlayer);
                if (nextHandEntity == null) return BadRequest(); // TODO Too late

                if (!Card.CanBeat(nextHandEntity.Cards, playedCards))
                {
                    logger.LogInformation("[{requestId}] Skipping {nextPlayer}: {reason}", requestId, nextPlayer, "Cannot beat");
                    continue;
                }

                logger.LogInformation("[{requestId}] Next turn is {nextPlayer}", requestId, nextPlayer);
                playEnd = false;
                break;
            }

            roundEntity.FreePlay = playEnd;

            if (playEnd)
            {
                logger.LogInformation("[{requestId}] Play cannot be beaten", requestId);
                var nextPlayer = player.Uuid;
                while (roundEntity.PlayerCards[roundEntity.TurnIndex] == 0)
                {
                    logger.LogInformation("[{requestId}] Skipping {nextPlayer}: {reason}", requestId, nextPlayer, "No cards");
                    roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count;
                    nextPlayer = roundEntity.PlayerUuids[roundEntity.TurnIndex];
                }

                nextHandEntity = await repository.GetHandAsync(gameEntity, nextPlayer);
                if (nextHandEntity == null) return BadRequest(); // TODO Too late
            }

            nextHandEntity.Turn = true;

            await repository.SaveHandAsync(nextHandEntity);
        }

        await repository.SaveRoundAsync(roundEntity);

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        return Accepted();
    }
}