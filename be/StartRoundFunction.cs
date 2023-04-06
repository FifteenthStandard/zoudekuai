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

        var requestId = Guid.NewGuid().ToString();
        var repository = new Repository(table, messages, logger, requestId);

        logger.LogInformation("[{requestId}] Request to start a round", requestId);

        var player = await GetPlayerFromRequest(req, table);

        var startRoundRequest = await JsonSerializer.DeserializeAsync<StartRoundRequest>(req.Body);
        var gameCode = startRoundRequest.GameCode;

        var gameEntity = await repository.GetGameAsync(gameCode);
        if (gameEntity == null) return BadRequest();

        if (player.Uuid != gameEntity.HostUuid)
        {
            logger.LogWarning("[{requestId}] Request failed: {reason}", requestId, "Not the host");
            return BadRequest("Not the host");
        }

        if (gameEntity.PlayerUuids.Count < 4)
        {
            logger.LogWarning("[{requstId}] Request failed: {reason}", requestId, "Not enough players");
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

        foreach (var handEntity in handEntities)
        {
            await repository.SaveHandAsync(handEntity);
        }

        await repository.SaveRoundAsync(roundEntity);
        await repository.SaveGameAsync(gameEntity);

        logger.LogInformation("[{requestId}] Completed successfully", requestId);

        return Accepted();
    }
}