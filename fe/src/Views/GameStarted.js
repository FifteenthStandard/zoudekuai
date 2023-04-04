import {
  IconButton,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  HourglassTop,
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

const Positions = [
  { left: 0, top: 100 },
  { left: 0, top: 200 },
  { right: 0, top: 200 },
  { right: 0, top: 100 },
];

export default function GameStarted() {
  const appState = useAppState();
  const appDispatch = useAppDispatch();

  const { strings, game, round, hand } = appState;

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
      round.players.map((player, index) =>
        <Paper key={index} sx={{ width: player.turn ? '50vw' : '45vw', position: 'absolute', ...Positions[index] }}>
          <Stack direction="row" padding={1} spacing={2} sx={{ display: 'flex', justifyContent: 'space-around' }}>
            {player.turn && <HourglassTop />}
            <Typography>{player.name}</Typography>
            {player.stole && <Report />}
            <Typography>{'🂠'.repeat(player.cards)}</Typography>
          </Stack>
        </Paper>
      )
    }
    <Paper sx={{ position: 'absolute', bottom: hand.turn ? 0 : -75, left: '50%', transform: 'translateX(-50%)' }}>
      <Stack direction="row" paddingInline={1} spacing={2}>
        {
          hand.cards.map(({ suit, value }, index) =>
            <Typography
              key={value}
              variant="h1"
              color={suit % 2 ? '#000' : '#f00'}
              onClick={() => hand.turn && appDispatch({ type: 'playCards', cardIndexes: [ index ] })}
            >
              {CardChars[value]}
            </Typography>
          )
        }
      </Stack>
    </Paper>
  </>
}