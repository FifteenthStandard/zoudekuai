import {
  Button,
  Paper,
  Stack,
  Typography,
} from '@mui/material';

import {
  useAppState,
  useAppDispatch,
} from '../AppContext';

export default function GameNotStarted() {
  const appState = useAppState();
  const appDispatch = useAppDispatch();

  const strings = appState.strings;

  const enoughPlayers = appState.game.players.length >= 4;

  const handleStartGame = () => {
    appDispatch({ type: 'startGame' });
  };

  return <>
    <Paper onClick={() => navigator.clipboard.writeText(appState.game.gameCode)}>
      <Typography padding={1} align="center">{strings.GameCode} {appState.game.gameCode}</Typography>
    </Paper>
    <Stack spacing={2} sx={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)' }}>
      <Typography align="center" color="text.secondary">
        {strings.Players}
      </Typography>
      {appState.game.players.map((player, ind) =>
        <Paper key={`${ind}-${player}`}>
          <Typography padding={1} align="center">{player}</Typography>
        </Paper>
      )}
      {
        appState.isHost
          ? <Button
              variant="contained"
              onClick={handleStartGame}
              disabled={!enoughPlayers}
            >
              {strings.StartGame}
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