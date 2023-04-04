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
  
  const { strings, game, round, hand } = appState;

  const discardRef = useRef(null);
  useEffect(function () {
    if (round.discard.length) {
      discardRef.current.lastChild.scrollIntoView();
      window.scroll(0, 0); // revert entire window scrolling
    }
  }, [round.discard])
  
  const [cardIndexes, setCardIndexes] = useState([]);

  const handleCardSelect = index => () => {
    if (!hand.turn) return;
    const foundIndex = cardIndexes.indexOf(index);
    setCardIndexes(
      foundIndex >= 0
        ? cardIndexes.slice(0, foundIndex).concat(cardIndexes.slice(foundIndex + 1))
        : [...cardIndexes, index]
    );
  };

  const handlePlayCards = () => {
    if (!hand.turn || cardIndexes.length === 0) return;
    appDispatch({ type: 'playCards', cardIndexes });
    setCardIndexes([]);
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
        <IconButton onClick={() => navigator.clipboard.writeText(game.gameCode)}>
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
            width: player.position !== null ? '25vw' : player.turn ? '50vw' : '45vw',
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
      hand.turn &&
        <Button
          sx={{ position: 'absolute', bottom: 100, left: '50%', transform: 'translateX(-50%)' }}
          disabled={cardIndexes.length === 0}
          onClick={handlePlayCards}
        >
          Play
        </Button>
    }
    <Paper sx={{ position: 'absolute', bottom: hand.turn ? 0 : -75, left: '50%', transform: 'translateX(-50%)', height: '100px' }}>
      <Stack direction="row" paddingInline={1}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={value}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={handleCardSelect(index)}
              marginTop={cardIndexes.includes(index) ? '-5px' : '0px'}
              marginBottom={cardIndexes.includes(index) ? '5px' : '0px'}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>
}