using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GameMessage
{
    [JsonPropertyName("gameCode")]
    public string GameCode { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameStatus Status { get; set; }

    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; }

    [JsonPropertyName("players")]
    public List<string> Players { get; set; } = new List<string>();
}

public class JoinMessage : GameMessage
{
    [JsonPropertyName("host")]
    public bool Host { get; set; }
}

public class RoundMessage
{
    public class Player
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("cards")]
        public int Cards { get; set; }
        [JsonPropertyName("turn")]
        public bool Turn { get; set; }
        [JsonPropertyName("stole")]
        public bool Stole { get; set; }
        [JsonPropertyName("position")]
        public int? Position { get; set; }
    }

    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoundStatus Status { get; set; }

    [JsonPropertyName("freePlay")]
    public bool FreePlay { get; set; }

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new List<Player>();

    [JsonPropertyName("discard")]
    public List<List<CardMessage>> Discard { get; set; } = new List<List<CardMessage>>();
}

public class HandMessage
{

    [JsonPropertyName("turn")]
    public bool Turn { get; set; }

    [JsonPropertyName("cards")]
    public List<CardMessage> Cards { get; set; } = new List<CardMessage>();
}

public class CardMessage
{
    [JsonPropertyName("suit")]
    public Suit Suit { get; set; }
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
    [JsonPropertyName("value")]
    public int Value { get; set; }
}
