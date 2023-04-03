using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

public class PlayerEntity : ITableEntity
{
    public const string PlayerPartitionKey = "Players";
    public string PartitionKey { get; set; } = PlayerPartitionKey;
    public string RowKey { get => Uuid; set => value.Noop(); }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Uuid { get; set; }
    public string Name { get; set; }
    public string ConnectionId { get; set; }
}

public class GameEntity : ITableEntity
{
    public const string GamePartitionKey = "Games";
    public string PartitionKey { get; set; } = GamePartitionKey;
    public string RowKey { get => GameCode; set => value.Noop(); }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string GameCode { get; set; }
    public string HostUuid { get; set; }
    public string HostName { get; set; }
    public GameStatus Status { get; set; }
    public int RoundNumber { get; set; }
    [IgnoreDataMember]
    public List<string> PlayerUuids { get; set; } = new List<string>();
    public string PlayerUuidsStr
    {
        get => PlayerUuids.Serialize();
        set => PlayerUuids = value.Deserialize();
    }
    [IgnoreDataMember]
    public List<string> PlayerNames { get; set; } = new List<string>();
    public string PlayerNamesStr
    {
        get => PlayerNames.Serialize();
        set => PlayerNames = value.Deserialize();
    }
}

public class RoundEntity : ITableEntity
{
    public string PartitionKey { get => GameCode; set => GameCode = value; }
    public string RowKey { get => RoundNumber.ToString(); set => value.Noop(); }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string GameCode { get; set; }
    public int RoundNumber { get; set; }
    public RoundStatus Status { get; set; }
    public int TurnIndex { get; set; }
    public int StoleIndex { get; set; }
    [IgnoreDataMember]
    public List<string> PlayerUuids { get; set; } = new List<string>();
    public string PlayerUuidsStr
    {
        get => PlayerUuids.Serialize();
        set => PlayerUuids = value.Deserialize();
    }
    [IgnoreDataMember]
    public List<string> PlayerNames { get; set; } = new List<string>();
    public string PlayerNamesStr
    {
        get => PlayerNames.Serialize();
        set => PlayerNames = value.Deserialize();
    }
}

public class HandEntity : ITableEntity
{
    public string PartitionKey { get => GameCode; set => GameCode = value; }
    public string RowKey { get => $"{RoundNumber}_{Uuid}"; set => value.Noop(); }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string GameCode { get; set; }
    public int RoundNumber { get; set; }
    public string Uuid { get; set; }
    public string Name { get; set; }
    public bool Turn { get; set; }
    [IgnoreDataMember]
    public List<Card> Cards { get; set; }
    public string CardsStr
    {
        get => Cards.Select(card => card.Value).Serialize();
        set => value.Deserialize<int>().Select(Card.FromValue);
    }
}
