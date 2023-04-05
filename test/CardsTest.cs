using Xunit;

public class CardsTest
{
    [Fact]
    public void TestSingleSameRankHigherSuit()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
        };
        var hand = new List<Card>
        {
            Card.FromValue(1),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestSingleSameRankLowerSuit()
    {
        var play = new List<Card>
        {
            Card.FromValue(1),
        };
        var hand = new List<Card>
        {
            Card.FromValue(0),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestSingleHigherRankLowerSuit()
    {
        var play = new List<Card>
        {
            Card.FromValue(3),
        };
        var hand = new List<Card>
        {
            Card.FromValue(4),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestSingleLowerRankHigherSuit()
    {
        var play = new List<Card>
        {
            Card.FromValue(4),
        };
        var hand = new List<Card>
        {
            Card.FromValue(3),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleSameRankBothHigher()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
        };
        var hand = new List<Card>
        {
            Card.FromValue(2),
            Card.FromValue(3),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleSameRankOneHigher()
    {
        var play = new List<Card>
        {
            Card.FromValue(1),
            Card.FromValue(2),
        };
        var hand = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(3),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleSameRankBothLower()
    {
        var play = new List<Card>
        {
            Card.FromValue(2),
            Card.FromValue(3),
        };
        var hand = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleSameRankOneLower()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(3),
        };
        var hand = new List<Card>
        {
            Card.FromValue(1),
            Card.FromValue(2),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleHigherRank()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
        };
        var hand = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(5),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleLowerRank()
    {
        var play = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(5),
        };
        var hand = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestDoubleNoDoubles()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
        };
        var hand = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(8),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestTripleHigherRank()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
            Card.FromValue(2),
        };
        var hand = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(5),
            Card.FromValue(6),
        };

        Assert.True(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestTripleLowerRank()
    {
        var play = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(5),
            Card.FromValue(6),
        };
        var hand = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
            Card.FromValue(2),
        };

        Assert.False(Card.CanBeat(hand, play));
    }

    [Fact]
    public void TestTripleNoTriples()
    {
        var play = new List<Card>
        {
            Card.FromValue(0),
            Card.FromValue(1),
            Card.FromValue(2),
        };
        var hand = new List<Card>
        {
            Card.FromValue(4),
            Card.FromValue(5),
            Card.FromValue(8),
        };

        Assert.False(Card.CanBeat(hand, play));
    }
}
