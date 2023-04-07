import {
  Button,
  Paper,
  Stack,
  Typography,
} from '@mui/material';

import {
  useAppState,
} from '../../AppContext';
import {
  GameCodePanel,
} from '../Shared';

export default function GameNotStarted() {
  const { client, game, isHost, strings } = useAppState();

  const enoughPlayers = game.players.length >= 4;

  const handleStartRound = async () => {
    try {
      await client.startRound(game.gameCode);
    } catch (error) {
      console.error(error);
    }
  };

  return <>
    <GameCodePanel />
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
        isHost
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