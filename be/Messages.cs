using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GameMessage
{
    [JsonPropertyName("gameCode")]
    public string GameCode { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameStatus Status { get; set; }

    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; }

    [JsonPropertyName("players")]
    public List<string> Players { get; set; } = new List<string>();
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
    }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoundStatus Status { get; set; }

    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; }

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new List<Player>();
}

public class HandMessage
{
    public class Card
    {
        [JsonPropertyName("suit")]
        public Suit Suit { get; set; }
        [JsonPropertyName("rank")]
        public int Rank { get; set; }
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    [JsonPropertyName("turn")]
    public bool Turn { get; set; }

    [JsonPropertyName("cards")]
    public List<Card> Cards { get; set; } = new List<Card>();
}
