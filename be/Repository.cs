using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

public class Repository
{
    private readonly TableClient _table;
    private readonly IAsyncCollector<SignalRMessage> _messages;
    private readonly ILogger _logger;
    private readonly string _requestId;

    public Repository(
        TableClient table,
        IAsyncCollector<SignalRMessage> messages,
        ILogger logger,
        string requestId)
    {
        _table = table;
        _messages = messages;
        _logger = logger;
        _requestId = requestId;
    }

    public async Task SavePlayerAsync(PlayerEntity playerEntity)
    {
        var uuid = playerEntity.Uuid;

        try
        {
            _logger.LogInformation("[{requestId}] Request to save player with UUID {uuid} started", _requestId, uuid);
            await _table.UpsertEntityAsync(playerEntity);
            _logger.LogInformation("[{requestId}] Request to save player with UUID {uuid} completed successfully", _requestId, uuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to save player with UUID {uuid} failed: {reason}", _requestId, uuid, ex.Message);
            throw;
        }
    }

    public async Task<GameEntity> GetGameAsync(string gameCode)
    {
        try
        {
            _logger.LogInformation("[{requestId}] Request to retrieve a game with Game Code {gameCode} started", _requestId, gameCode);
            var gameEntity = await _table.GetEntityAsync<GameEntity>(GameEntity.GamePartitionKey, gameCode);
            if (gameEntity == null)
            {
                _logger.LogWarning("[{requestId}] Request to retrieve a game with Game Code {gameCode} failed: {reason}", _requestId, gameCode, "Game not found");
                return null;
            }
            _logger.LogInformation("[{requestId}] Request to retrieve a game with Game Code {gameCode} completed successfully", _requestId, gameCode);
            return gameEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to retrieve a game with Game Code {gameCode} failed: {reason}", _requestId, gameCode, ex.Message);
            throw;
        }
    }

    public async Task SaveGameAsync(GameEntity gameEntity, params string[] messageArguments)
    {
        var gameCode = gameEntity.GameCode;

        try
        {
            _logger.LogInformation("[{requestId}] Request to save game with Game Code {gameCode} started", _requestId, gameCode);
            await _table.UpsertEntityAsync(gameEntity);
            _logger.LogInformation("[{requestId}] Request to save game with Game Code {gameCode} completed successfully", _requestId, gameCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{requestId}] Request to save game with Game Code {gameCode} failed: {reason}", gameCode, ex.Message);
            throw;
        }

        var gameMessage = new GameMessage
        {
            GameCode = gameEntity.GameCode,
            Status = gameEntity.Status,
            RoundNumber = gameEntity.RoundNumber,
            Players = gameEntity.PlayerNames,
        };

        var arguments = new []
            {
                JsonSerializer.Serialize(gameMessage)
            }
            .Concat(messageArguments)
            .ToArray();

        await _messages.AddAsync(
            new SignalRMessage
            {
                Target = "gameUpdate",
                GroupName = gameEntity.GameCode,
                Arguments = arguments,
            });
    }

    public async Task<RoundEntity> GetCurrentRoundAsync(GameEntity game)
    {
        var gameCode = game.GameCode;
        var roundNumber = game.RoundNumber.ToString();

        try
        {
            _logger.LogInformation("[{requestId}] Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} started", _requestId, gameCode, roundNumber);
            var roundEntity = await _table.GetEntityAsync<RoundEntity>(gameCode, roundNumber);
            if (roundEntity == null)
            {
                _logger.LogWarning("[{requestId}] Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} failed: {reason}", _requestId, gameCode, roundNumber, "Round not found");
                return null;
            }
            _logger.LogInformation("[{requestId}] Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} completed successfully", _requestId, gameCode, roundNumber);
            return roundEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to retrieve a round with Game Code {gameCode} and Round Number {roundNumber} failed: {reason}", _requestId, gameCode, roundNumber, ex.Message);
            throw;
        }
    }

    public async Task SaveRoundAsync(RoundEntity roundEntity, params string[] messageArguments)
    {
        var gameCode = roundEntity.GameCode;
        var roundNumber = roundEntity.RoundNumber.ToString();

        try
        {
            _logger.LogInformation("[{requestId}] Request to save round with Game Code {gameCode} and Round Number {roundNumber} started", _requestId, gameCode, roundNumber);
            await _table.UpsertEntityAsync(roundEntity);
            _logger.LogInformation("[{requestId}] Request to save round with Game Code {gameCode} and Round Number {roundNumber} completed successfully", _requestId, gameCode, roundNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to save round with Game Code {gameCode} and Round Number {roundNumber} failed: {reason}", _requestId, gameCode, roundNumber, ex.Message);
            throw;
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

        var arguments = new []
            {
                JsonSerializer.Serialize(roundMessage)
            }
            .Concat(messageArguments)
            .ToArray();

        await _messages.AddAsync(
            new SignalRMessage
            {
                Target = "roundUpdate",
                GroupName = gameCode,
                Arguments = arguments,
            });
    }

    public async Task<HandEntity> GetHandAsync(GameEntity game, string playerUuid)
    {
        var gameCode = game.GameCode;
        var roundNumber = game.RoundNumber.ToString();

        try
        {
            _logger.LogInformation("[{requestId}] Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} started", _requestId, gameCode, roundNumber, playerUuid);
            var handEntity = await _table.GetEntityAsync<HandEntity>(gameCode, $"{roundNumber}_{playerUuid}");
            if (handEntity == null)
            {
                _logger.LogWarning("[{requestId}] Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} failed: {reason}", _requestId, gameCode, roundNumber, playerUuid, "Hand not found");
                return null;
            }
            _logger.LogInformation("[{requestId}] Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} completed successfully", _requestId, gameCode, roundNumber, playerUuid);
            return handEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to retrieve a hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} failed: {reason}", _requestId, gameCode, roundNumber, playerUuid, ex.Message);
            throw;
        }
    }

    public async Task SaveHandAsync(HandEntity handEntity)
    {
        var gameCode = handEntity.GameCode;
        var roundNumber = handEntity.RoundNumber;
        var uuid = handEntity.Uuid;

        try
        {
            _logger.LogInformation("[{requestId}] Request to save hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} started", _requestId, gameCode, roundNumber, uuid);
            await _table.UpsertEntityAsync(handEntity);
            _logger.LogInformation("[{requestId}] Request to save hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} completed successfully", _requestId, gameCode, roundNumber, uuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{requestId}] Request to save hand with Game Code {gameCode} and Round Number {roundNumber} for {uuid} failed: {reason}", _requestId, gameCode, roundNumber, uuid, ex.Message);
            throw;
        }

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

        await _messages.AddAsync(
            new SignalRMessage
            {
                Target = "handUpdate",
                GroupName = handEntity.Uuid,
                Arguments = new [] { JsonSerializer.Serialize(handMessage) },
            });
    }
}
