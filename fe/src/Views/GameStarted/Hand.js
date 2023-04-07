import {
  useEffect,
  useState,
} from 'react';
import {
  Button,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  useAppState,
} from "../../AppContext"
import {
  CardChars,
} from '../Shared';

export default function Hand() {
  const {
    client,
    game,
    hand,
    round,
    strings,
  } = useAppState();

  const [cardIndexes, setCardIndexes] = useState([]);

  useEffect(() => {
    if (hand.cards[0] && hand.cards[0].value === 0) setCardIndexes([0]);
  }, [hand.cards]);

  const lastPlay = round.discard.length > 0 ? round.discard[round.discard.length-1] : null;

  const playableCards = hand.cards.map(({ rank, value }, index) => {
    if (cardIndexes.includes(index)) return true;
    if (cardIndexes.length > 0 && hand.cards[cardIndexes[0]].rank !== rank) return false;
    if (!round.freePlay && lastPlay && cardIndexes.length === lastPlay.length) return false;
    if (!hand.turn || round.freePlay || !lastPlay) return true;
    if (lastPlay.length === 1) return value > lastPlay[0].value;
    const cardsInRank = hand.cards.filter(card => card.rank === rank).reverse();
    return cardsInRank.length >= lastPlay.length &&
      (rank > lastPlay[0].rank ||
        (rank === lastPlay[0].rank && cardsInRank[0].value > lastPlay[0].value));
  });

  const invalidPlay = cardIndexes.length === 0 || (!round.freePlay &&
    (cardIndexes.length !== lastPlay.length ||
      hand.cards[cardIndexes[0]].value < lastPlay[0].value));

  const handleCardSelect = index => () => {
    if (!hand.turn) return;
    if (index === 0 && hand.cards[0].value === 0) return;
    const foundIndex = cardIndexes.indexOf(index);
    let newIndexes;
    if (foundIndex >= 0) {
      newIndexes = cardIndexes.slice(0, foundIndex).concat(cardIndexes.slice(foundIndex + 1));
    } else if (cardIndexes.length === 0 || hand.cards[index].rank === hand.cards[cardIndexes[0]].rank) {
      newIndexes = [...cardIndexes, index];
    } else {
      newIndexes = cardIndexes;
    }
    newIndexes.sort((i, j) => j - i);
    setCardIndexes(newIndexes);
  };

  const handlePlayCards = async () => {
    if (!hand.turn || cardIndexes.length === 0) return;
    try {
      const play = [...cardIndexes];
      setCardIndexes([]);
      await client.playCards(game.gameCode, play);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    {
      hand.turn && round.freePlay &&
        <Typography
          padding={'6px'}
          sx={{ position: 'absolute', bottom: 100 }}
          color="text.secondary"
        >
          {strings.FreePlay}
        </Typography>
    }
    {
      hand.turn &&
        <Button
          sx={{ position: 'absolute', bottom: 100, left: '50%', transform: 'translateX(-50%)' }}
          disabled={invalidPlay}
          onClick={handlePlayCards}
        >
          {strings.Play}
        </Button>
    }
    {
      hand.cards.length > 0 && <Paper sx={{ position: 'absolute', bottom: hand.turn ? 0 : -50, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
        <Stack direction="row" paddingInline={1}>
          {
            hand.cards.map(({ suit, value }, index) =>
              <Typography
                key={`${index}-${value}`}
                variant="h1"
                color={suit % 2 ? '#000' : '#f00'}
                onClick={handleCardSelect(index)}
                marginTop={cardIndexes.includes(index) ? '-5px' : playableCards[index] ? '0px' : '50px'}
                marginBottom={cardIndexes.includes(index) ? '5px' : playableCards[index] ? '0px' : '-50px'}
              >
                {CardChars[value]}
              </Typography>
            )
          }
        </Stack>
      </Paper>
    }
  </>
};
