import {
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
} from '../../AppContext';

export default function GameCodePanel() {
  const { game, name, strings } = useAppState();

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

  return <Paper>
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
}