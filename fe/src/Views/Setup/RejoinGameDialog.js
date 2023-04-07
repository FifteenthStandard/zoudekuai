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
  useAppState,
} from '../../AppContext';

export default function RejoinGameDialog({ open, setOpen, name, storedGameCode }) {
  const { client, strings } = useAppState();

  const handleSubmit = async (ev) => {
    ev.preventDefault();
    try {
      await client.joinGame(name, storedGameCode);
    } catch (error) {
      console.error(error);
    }
  };

  return <Dialog
    open={open}
    onClose={() => setOpen(false)}
  >
    <form onSubmit={handleSubmit}>
      <DialogTitle>{strings.RejoinGame}</DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <DialogContentText>{strings.RejoinGameDialog}</DialogContentText>
          <TextField
            label={strings.GameCode}
            value={storedGameCode}
            autoFocus
            readOnly
            />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => setOpen(false)}>{strings.Cancel}</Button>
        <Button onClick={handleSubmit}>{strings.RejoinGame}</Button>
        <input type="submit" hidden />
      </DialogActions>
    </form>
  </Dialog>;
};
