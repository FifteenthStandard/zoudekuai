import {
  useEffect,
  useRef,
  useState,
} from 'react';
import {
  Button,
  IconButton,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  HourglassTop,
  LooksOne,
  LooksTwo,
  Looks3,
  Looks4,
  Looks5,
  Report,
  Share,
  Sync,
} from '@mui/icons-material';

import {
  useAppState,
  useAppDispatch,
} from '../AppContext';

const CardChars = [
  '🃁','🃑','🂱','🂡',
  '🃂','🃒','🂲','🂢',
  '🃃','🃓','🂳','🂣',
  '🃄','🃔','🂴','🂤',
  '🃅','🃕','🂵','🂥',
  '🃆','🃖','🂶','🂦',
  '🃇','🃗','🂷','🂧',
  '🃈','🃘','🂸','🂨',
  '🃉','🃙','🂹','🂩',
  '🃊','🃚','🂺','🂪',
  '🃋','🃛','🂻','🂫',
  '🃍','🃝','🂽','🂭',
  '🃎','🃞','🂾','🂮',
  '🂠',
];

const PlayerCardPositions = [
  { left: 0, top: 100 },
  { left: 0, top: 200 },
  { right: 0, top: 200 },
  { right: 0, top: 100 },
];

const PositionIcons = [
  <LooksOne key="position" />,
  <LooksTwo key="position" />,
  <Looks3 key="position" />,
  <Looks4 key="position" />,
  <Looks5 key="position" />
];

export default function GameStarted() {
  const appState = useAppState();
  const appDispatch = useAppDispatch();
  
  const { strings, name, game, round, hand, isHost } = appState;

  const discardRef = useRef(null);
  useEffect(function () {
    if (round.discard.length) {
      discardRef.current.lastChild.scrollIntoView();
      window.scroll(0, 0); // revert entire window scrolling
    }
  }, [round.discard])
  
  const [cardIndexes, setCardIndexes] = useState([]);

  useEffect(() => {
    if (hand.cards[0] && hand.cards[0].value === 0) setCardIndexes([0]);
  }, [hand.cards]);

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

  const lastPlay = round.discard.length > 0 ? round.discard[round.discard.length-1] : null;
  const invalidPlay = cardIndexes.length === 0 || (!round.freePlay &&
    (cardIndexes.length !== lastPlay.length ||
      hand.cards[cardIndexes[0]].value < lastPlay[0].value));

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

  const handlePlayCards = () => {
    if (!hand.turn || cardIndexes.length === 0) return;
    appDispatch({ type: 'playCards', cardIndexes });
    setCardIndexes([]);
  };

  const handleStartRound = () => {
    appDispatch({ type: 'startRound' });
  };

  const shareGameCode = () => {
    const url = `${window.location.origin}${window.location.pathname}#${game.gameCode}`;
    const share = {
      title: strings.Title,
      text: strings.Share(name),
      url,
    };
    if (navigator.canShare && navigator.canShare(share)) {
      navigator.share(share);
    } else {
      navigator.clipboard.writeText(url);
    }
  };

  return <>
    <Paper>
      <Stack padding={1} spacing={2} direction="row" sx={{ display: 'flex', justifyContent: 'space-around', alignItems: 'center' }}>
        <Typography fontSize={16}>
          {strings.GameCode}
        </Typography>
        <Typography fontSize={16}>
          {game.gameCode}
        </Typography>
        <IconButton onClick={shareGameCode}>
          <Share />
        </IconButton>
      </Stack>
    </Paper>
    {
      round.players.map((player, index) => {
        const elems = [
          <Typography key="name">{player.name}</Typography>,
          player.stole ? <Report key="stole" /> : null,
          PositionIcons[player.position] || <Typography key="cards">{'🂠'.repeat(player.cards)}</Typography>,
          player.turn ? <HourglassTop key="turn" /> : null,
        ];
        if (index >= 2) elems.reverse();
        return <Paper
          key={index}
          sx={{
            width: typeof(player.position) === 'number' ? '25vw' : player.turn ? '50vw' : '45vw',
            position: 'absolute',
            ...PlayerCardPositions[index]
          }}
        >
          <Stack direction="row" padding={1} spacing={2} sx={{ display: 'flex', justifyContent: 'space-around' }}>
            {elems}
          </Stack>
        </Paper>
      })
    }
    <Sync sx={{ position: 'absolute', top: 170, left: '50%', transform: 'translate(-50%, -50%)' }} />
    <Stack ref={discardRef} direction="column" spacing={1}
      sx={{ position: 'absolute', top: 300, height: 'calc(100vh - 450px)', width: '100%', overflowY: 'scroll', display: 'flex', alignItems: 'center' }}
    >
      {
        round.discard.map((cards, index) =>
          <Paper key={index} sx={{ width: 'fit-content' }}>
            <Stack direction="row" paddingInline={1}>
              {
                cards.map(({ suit, value }) =>
                  <Typography
                    key={value}
                    variant="h1"
                    color={suit % 2 ? '#000' : '#f00'}
                  >
                    {CardChars[value]}
                  </Typography>
                )
              }
            </Stack>
          </Paper>
        )
      }
    </Stack>
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
    {
      round.status === 'Finished' && (
        isHost
          ? <Button
              variant="contained"
              sx={{ position: 'absolute', bottom: 50, left: '50%', transform: 'translateX(-50%)' }}
              onClick={handleStartRound}
            >
              {strings.StartRound}
            </Button>
          : <Typography
              padding={1}
              color="text.secondary"
              sx={{ position: 'absolute', bottom: 50, left: '50%', transform: 'translateX(-50%)' }}
            >
              {strings.WaitingForHost}
            </Typography>
      )
    }
  </>
}