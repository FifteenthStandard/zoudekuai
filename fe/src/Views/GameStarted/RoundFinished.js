import {
  Button,
  Typography,
} from '@mui/material';
import { useAppState } from "../../AppContext";

export default function RoundFinished() {
  const { client, game, isHost, strings } = useAppState();

  const handleStartRound = async () => {
    try {
      await client.startRound(game.gameCode);
    } catch (error) {
      console.error(error);
    }
  };

  return isHost
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
      </Typography>;
};
