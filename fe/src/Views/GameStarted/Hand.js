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

function HandNotTurn() {
  const { hand } = useAppState();

  return hand.cards.length > 0 &&
    <Paper sx={{ position: 'absolute', bottom: -50, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={`${index}-${value}`}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>;
};

function HandSteal() {
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

  const playableCards = hand.cards.map(({ rank }, index) => {
    if (cardIndexes.includes(index)) return true;
    if (rank !== 0) return false;
    return true;
  });

  const invalidPlay = steal => (steal && round.freePlay)
    ? cardIndexes.length === 0
    : round.freePlay
    ? cardIndexes.length !== 1
    : steal
    ? false
    : cardIndexes.length !== 0;

  const handleCardSelect = index => () => {
    if (index === 0 && hand.cards[0].value === 0) return;
    if (!playableCards[index]) return;
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

  const handleSteal = steal => async () => {
    if (invalidPlay(steal)) return;
    try {
      const play = [...cardIndexes];
      setCardIndexes([]);
      await client.steal(game.gameCode, steal, play);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    <Button
      sx={{ position: 'absolute', bottom: 100, left: '25%', transform: 'translateX(-50%)' }}
      disabled={invalidPlay(false)}
      onClick={handleSteal(false)}
    >
      {strings.Pass}
    </Button>
    <Button
      sx={{ position: 'absolute', bottom: 100, left: '75%', transform: 'translateX(-50%)' }}
      disabled={invalidPlay(true)}
      onClick={handleSteal(true)}
    >
      {strings.Steal}
    </Button>
    <Paper sx={{ position: 'absolute', bottom: 0, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={`${index}-${value}`}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={handleCardSelect(index)}
              marginTop={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '50px'}
              marginBottom={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '-50px'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>
};

function HandContinue() {
  const {
    client,
    game,
    hand,
    strings,
  } = useAppState();

  const [cardIndexes, setCardIndexes] = useState([]);

  const playableCards = hand.cards.map(({ rank }, index) => {
    if (cardIndexes.includes(index)) return true;
    if (rank !== 0) return false;
    return true;
  });

  const handleCardSelect = index => () => {
    if (index === 0 && hand.cards[0].value === 0) return;
    if (!playableCards[index]) return;
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
    try {
      const play = [...cardIndexes];
      setCardIndexes([]);
      await client.playCards(game.gameCode, play);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    <Button
      sx={{ position: 'absolute', bottom: 100, left: '50%', transform: 'translateX(-50%)' }}
      onClick={handlePlayCards}
    >
      {strings.Confirm}
    </Button>
    <Paper sx={{ position: 'absolute', bottom: 0, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={`${index}-${value}`}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={handleCardSelect(index)}
              marginTop={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '50px'}
              marginBottom={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '-50px'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>
};

function HandFreePlay() {
  const {
    client,
    game,
    hand,
    strings,
  } = useAppState();

  const [cardIndexes, setCardIndexes] = useState([]);

  useEffect(() => {
    if (hand.cards[0] && hand.cards[0].value === 0) setCardIndexes([0]);
  }, [hand.cards]);

  const playableCards = hand.cards.map(({ rank }, index) => {
    if (cardIndexes.includes(index)) return true;
    if (cardIndexes.length > 0 && hand.cards[cardIndexes[0]].rank !== rank) return false;
    return true;
  });

  const invalidPlay = cardIndexes.length === 0;

  const handleCardSelect = index => () => {
    if (index === 0 && hand.cards[0].value === 0) return;
    if (!playableCards[index]) return;
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
    if (cardIndexes.length === 0) return;
    try {
      const play = [...cardIndexes];
      setCardIndexes([]);
      await client.playCards(game.gameCode, play);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    <Typography
      padding={'6px'}
      sx={{ position: 'absolute', bottom: 100 }}
      color="text.secondary"
    >
      {strings.FreePlay}
    </Typography>
    <Button
      sx={{ position: 'absolute', bottom: 100, left: '50%', transform: 'translateX(-50%)' }}
      disabled={invalidPlay}
      onClick={handlePlayCards}
    >
      {strings.Play}
    </Button>
    <Paper sx={{ position: 'absolute', bottom: 0, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={`${index}-${value}`}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={handleCardSelect(index)}
              marginTop={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '50px'}
              marginBottom={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '-50px'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>;
};

function HandNormalTurn() {
  const {
    client,
    game,
    hand,
    round,
    strings,
  } = useAppState();

  const [cardIndexes, setCardIndexes] = useState([]);

  const lastPlay = round.discard.length > 0 ? round.discard[round.discard.length-1] : null;

  const playableCards = hand.cards.map(({ rank, value }, index) => {
    if (cardIndexes.includes(index)) return true;
    if (cardIndexes.length > 0 && hand.cards[cardIndexes[0]].rank !== rank) return false;
    if (cardIndexes.length === lastPlay.length) return false;
    if (lastPlay.length === 1) return value > lastPlay[0].value;
    const cardsInRank = hand.cards.filter(card => card.rank === rank).reverse();
    return cardsInRank.length >= lastPlay.length &&
      (rank > lastPlay[0].rank ||
        (rank === lastPlay[0].rank && cardsInRank[0].value > lastPlay[0].value));
  });

  const invalidPlay = cardIndexes.length === 0 ||
    cardIndexes.length !== lastPlay.length ||
    hand.cards[cardIndexes[0]].value < lastPlay[0].value;

  const handleCardSelect = index => () => {
    if (!playableCards[index]) return;
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
    if (cardIndexes.length === 0) return;
    try {
      const play = [...cardIndexes];
      setCardIndexes([]);
      await client.playCards(game.gameCode, play);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    <Button
      sx={{ position: 'absolute', bottom: 100, left: '50%', transform: 'translateX(-50%)' }}
      disabled={invalidPlay}
      onClick={handlePlayCards}
    >
      {strings.Play}
    </Button>
    <Paper sx={{ position: 'absolute', bottom: 0, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={`${index}-${value}`}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={handleCardSelect(index)}
              marginTop={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '50px'}
              marginBottom={cardIndexes.includes(index) ? '0px' : playableCards[index] ? '25px' : '-50px'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>;
};

export default function Hand() {
  const {
    hand,
    round,
  } = useAppState();

  return !hand.turn
    ? <HandNotTurn />
    : round.stealChance
    ? <HandSteal />
    : round.firstPlayContinuation
    ? <HandContinue />
    : round.freePlay
    ? <HandFreePlay />
    : <HandNormalTurn />;
};
