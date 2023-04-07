import {
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

  return <>
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
  </>
};
