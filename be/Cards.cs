using System;
using System.Collections.Generic;
using System.Linq;

public class Card
{
    public Suit Suit { get; set; }
    public int Rank { get; set; }
    public int Value { get; set; }

    public static Card FromValue(int value)
        => new Card
        {
            Suit = (Suit) (value % 4),
            Rank = value / 4,
            Value = value,
        };
}

public class Deck
{
    private readonly List<int> _cards;
    private readonly Random _random;

    public Deck(int players)
    {
        _cards = Enumerable.Range(0, players * 6).ToList();
        _random = new Random();
    }

    public List<Card> Deal()
    {
        var hand = new List<Card>();
        for (var handIndex = 0; handIndex < 6; handIndex++)
        {
            var cardIndex = _random.Next(_cards.Count);
            hand.Add(Card.FromValue(_cards[cardIndex]));
            _cards.RemoveAt(cardIndex);
        }
        hand = hand.OrderBy(card => card.Value).ToList();
        return hand;
    }
}