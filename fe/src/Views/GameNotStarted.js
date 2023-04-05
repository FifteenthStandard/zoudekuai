import {
  Button,
  IconButton,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import {
  Share,
} from '@mui/icons-material';

import {
  useAppState,
  useAppDispatch,
} from '../AppContext';

export default function GameNotStarted() {
  const appState = useAppState();
  const appDispatch = useAppDispatch();

  const { strings, game } = appState;

  const enoughPlayers = game.players.length >= 4;

  const handleStartRound = () => {
    appDispatch({ type: 'startRound' });
  };

  return <>
    {
      game.gameCode
        ? <Paper>
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
        : <Paper>
          <Typography padding={1} fontSize={16} align="center">
            Creating game...
          </Typography>
        </Paper>
    }
    <Stack spacing={2} sx={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)' }}>
      <Typography align="center" color="text.secondary">
        {strings.Players}
      </Typography>
      {game.players.map((player, ind) =>
        <Paper key={`${ind}-${player}`}>
          <Typography padding={1} align="center">{player}</Typography>
        </Paper>
      )}
      {
        appState.isHost
          ? <Button
              variant="contained"
              onClick={handleStartRound}
              disabled={!enoughPlayers}
            >
              {enoughPlayers ? strings.StartRound : strings.WaitingForPlayers}
            </Button>
          : <Paper>
              <Typography padding={1} color="text.secondary">
                {enoughPlayers ? strings.WaitingForHost : strings.WaitingForPlayers}
              </Typography>
            </Paper>
      }
    </Stack>
  </>;
}