import {
  useState,
} from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';

import {
  getInitialValue,
  useAppState,
 } from '../../AppContext';

export default function JoinGameDialog({ open, setOpen, name }) {
  const { client, strings } = useAppState();

  const [gameCode, setGameCode] = useState(getInitialValue('gameCode', '', true));
  const [gameCodeError, setGameCodeError] = useState('');

  const handleChange = (ev) => {
    if (ev.target.value.match(/^\d{0,4}$/)) {
      setGameCode(ev.target.value);
      setGameCodeError('');
    }
  };

  const handleSubmit = async (ev) => {
    ev.preventDefault();
    if (!gameCode) {
      setGameCodeError(strings.GameCodeRequired);
      return;
    }
    try {
      await client.joinGame(name, gameCode);
    } catch (error) {
      console.error(error);
      setGameCodeError('Invalid game code');
    }
  };

  return <Dialog
      open={open}
      onClose={() => setOpen(false)}
    >
    <form onSubmit={handleSubmit}>
      <DialogTitle>{strings.JoinGame}</DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText>{strings.JoinGameDialog}</DialogContentText>
          <TextField
            label={strings.GameCode}
            required
            inputProps={{ inputMode: 'numeric' }}
            value={gameCode}
            onChange={handleChange}
            autoFocus
            error={!!gameCodeError}
            helperText={gameCodeError}
            />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => setOpen(false)}>{strings.Cancel}</Button>
        <Button onClick={handleSubmit}>{strings.JoinGame}</Button>
        <input type="submit" hidden />
      </DialogActions>
    </form>
  </Dialog>
};
