import {
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  LooksOne,
  LooksTwo,
  Looks3,
  Looks4,
  Looks5,
  Report,
  Sync,
} from '@mui/icons-material';

import {
  useAppState,
} from '../../AppContext';

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

export default function Players() {
  const { round } = useAppState();

  const roundFinished = round.status === 'Finished';

  return <>
    {
      round.players.map((player, index) => {
        const elems = [
          <Typography key="name">{player.name}</Typography>,
          player.stole && !roundFinished ? <Report key="stole" /> : null,
          PositionIcons[player.position] || (!roundFinished && <Typography key="cards">{'🂠'.repeat(player.cards)}</Typography>),
          player.turn ? <CircularProgress key="turn" size="24px" /> : null,
        ];

        if (round.status === 'Finished') {
          elems.push(<Typography key="score">¥{player.score}</Typography>)
        }

        if (index >= 2) elems.reverse();

        return <Paper
          key={index}
          sx={{
            width: roundFinished ? '35vw' : typeof(player.position) === 'number' ? '25vw' : player.turn ? '50vw' : '45vw',
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
  </>
};
