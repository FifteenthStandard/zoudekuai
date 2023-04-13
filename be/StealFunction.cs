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

public class StealFunction : FunctionBase
{
    public class StealRequest
    {
        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }
        [JsonPropertyName("steal")]
        public bool Steal { get; set; }
        [JsonPropertyName("cardIndexes")]
        public List<int> CardIndexes { get; set; }
    }

    [FunctionName("steal")]
    public async Task<IActionResult> Steal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options")] HttpRequest req,
        [Table("zoudekuai")] TableClient table,
        [SignalR(HubName = "zoudekuai")] IAsyncCollector<SignalRMessage> messages,
        ILogger logger)
    {
        SetCorsHeaders(req);
        if (IsCorsPreflight(req)) return Ok();

        var requestId = Guid.NewGuid().ToString();
        var repository = new Repository(table, messages, logger, requestId);

        logger.LogInformation("[{requestId}] Request to steal", requestId);

        var player = await GetPlayerFromRequest(req, table);

        var stealRequest = await JsonSerializer.DeserializeAsync<StealRequest>(req.Body);
        var gameCode = stealRequest.GameCode;

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

        if (roundEntity.StealChances == 0)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Not a chance to steal");
            return BadRequest("Not a chance to steal");
        }

        var handEntity = await repository.GetHandAsync(gameEntity, player.Uuid);
        if (handEntity == null) return BadRequest();

        var playedCardIndexes = stealRequest.CardIndexes.OrderByDescending(i => i).Distinct().ToList();
        var playedCards = playedCardIndexes.Select(index => handEntity.Cards[index]).ToList();

        if (stealRequest.Steal)
        {
            logger.LogInformation("[{requestId}] Steal played", requestId);

            roundEntity.StoleIndex = roundEntity.TurnIndex;

            if (roundEntity.FreePlay && !playedCards.Contains(Card.FromValue(0)))
            {
                logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Must play Ace of Diamonds");
                return BadRequest("Must play Ace of Diamonds");
            }

            int rank = 0;
            foreach (var cardIndex in playedCardIndexes)
            {
                if (cardIndex < 0 || cardIndex >= handEntity.Cards.Count)
                {
                    logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Invalid card index");
                    return BadRequest("Invalid card index");
                }

                if (handEntity.Cards[cardIndex].Rank != rank)
                {
                    logger.LogWarning("Request failed: {reason}", requestId, "Cards don't match");
                    return BadRequest("Cards don't match");
                }

                handEntity.Cards.RemoveAt(cardIndex);
            }

            if (roundEntity.FreePlay)
            {
                roundEntity.Discard.Add(playedCards);
            }
            else
            {
                roundEntity.Discard[0] = playedCards.Concat(roundEntity.Discard[0]).ToList();
                playedCards.Add(Card.FromValue(0));
            }

            handEntity.Turn = false;
            await repository.SaveHandAsync(handEntity);

            roundEntity.PlayerCards[roundEntity.TurnIndex] -= playedCardIndexes.Count;

            roundEntity.FreePlay = false;
            roundEntity.StealChances = 0;

            var startingTurn = roundEntity.TurnIndex;
            HandEntity nextHandEntity = handEntity;
            bool playEnd = true;
            for (
                roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count;
                roundEntity.TurnIndex != startingTurn;
                roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count)
            {
                var nextPlayer = roundEntity.PlayerUuids[roundEntity.TurnIndex];

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
                nextHandEntity = await repository.GetHandAsync(gameEntity, nextPlayer);
                if (nextHandEntity == null) return BadRequest(); // TODO Too late
            }

            nextHandEntity.Turn = true;

            await repository.SaveHandAsync(nextHandEntity);
        }
        else
        {
            logger.LogInformation("[{requestId}] Steal passed", requestId);

            if (roundEntity.FreePlay && playedCards.Count != 1 && !playedCards.Contains(Card.FromValue(0)))
            {
                logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Must play Ace of Diamonds");
                return BadRequest("Must play Ace of Diamonds");
            }
            else if (!roundEntity.FreePlay && playedCards.Count > 0)
            {
                logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Cannot play cards when passing");
                return BadRequest("Cannot play cards when passing");
            }

            if (roundEntity.FreePlay)
            {
                foreach (var cardIndex in playedCardIndexes)
                {
                    handEntity.Cards.RemoveAt(cardIndex);
                }

                roundEntity.Discard.Add(playedCards);
                roundEntity.PlayerCards[roundEntity.TurnIndex] -= playedCardIndexes.Count;
            }

            handEntity.Turn = false;
            await repository.SaveHandAsync(handEntity);

            roundEntity.FreePlay = false;
            roundEntity.StealChances -= 1;
            roundEntity.FirstPlayContinuation = roundEntity.StealChances == 0;
            roundEntity.TurnIndex = (roundEntity.TurnIndex + 1) % roundEntity.PlayerUuids.Count;
            var nextPlayer = roundEntity.PlayerUuids[roundEntity.TurnIndex];

            var nextHandEntity = await repository.GetHandAsync(gameEntity, nextPlayer);
            if (nextHandEntity == null) return BadRequest(); // TODO Too late

            nextHandEntity.Turn = true;
            await repository.SaveHandAsync(nextHandEntity);
        }

        await repository.SaveRoundAsync(roundEntity);

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        return Accepted();
    }
}