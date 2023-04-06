import React, {
  useState,
} from 'react';
import {
  Button,
  ButtonGroup,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Stack,
  Paper,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material';
import {
  getInitialValue,
  useAppState,
  useAppDispatch,
} from '../AppContext';

export default function Start() {
  const appState = useAppState();
  const appDispatch = useAppDispatch();

  const strings = appState.strings;

  const [name, setName] = useState(getInitialValue('name', ''));
  const [nameError, setNameError] = useState('');

  const handleNewGame = () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    appDispatch({ type: 'newGame', name });
  };

  const [joinGameOpen, setJoinGameOpen] = useState(false);
  const handleJoinGameOpen = () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    setJoinGameOpen(true);
  };

  const [gameCode, setGameCode] = useState(getInitialValue('gameCode', '', true));
  const [gameCodeError, setGameCodeError] = useState('');

  const handleCodeChange = (ev) => {
    if (ev.target.value.match(/^\d{0,4}$/)) {
      setGameCode(ev.target.value);
      setGameCodeError('');
    }
  };

  const handleJoinGameSubmit = (ev) => {
    ev.preventDefault();
    if (!gameCode) {
      setGameCodeError(strings.GameCodeRequired);
      return;
    }
    appDispatch({ type: 'joinGame', name, gameCode });
  };

  const storedGameCode = getInitialValue('gameCode', '');
  const [rejoinGameOpen, setRejoinGameOpen] = useState(!!storedGameCode);

  const handleRejoinGameSubmit = (ev) => {
    ev.preventDefault();
    appDispatch({ type: 'joinGame', name, gameCode: storedGameCode });
  };

  const center = {position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)'};

  return <Paper sx={{ width: '80vw', margin: 'auto', ...center }}>
    <Stack spacing={2} padding={2}>
      <Typography variant="h3" textAlign="center">{strings.Title}</Typography>
      <ToggleButtonGroup
        exclusive
        value={appState.lang}
        onChange={(_, lang) => appDispatch({ type: 'setLang', lang })}
        color="primary"
        fullWidth
      >
        <ToggleButton value="zh">中文</ToggleButton>
        <ToggleButton value="en">EN</ToggleButton>
      </ToggleButtonGroup>
      <TextField
        label={strings.Name}
        required
        value={name}
        onChange={ev => setName(ev.target.value) || setNameError('')}
        error={!!nameError}
        helperText={nameError}
        fullWidth
        autoFocus
      />
      <ButtonGroup fullWidth variant="contained">
        <Button onClick={handleNewGame}>
          {strings.NewGame}
        </Button>
        <Button onClick={handleJoinGameOpen}>
          {strings.JoinGame}
        </Button>
      </ButtonGroup>
    </Stack>
    <Dialog
      open={joinGameOpen}
      onClose={() => setJoinGameOpen(false)}
    >
      <form onSubmit={handleJoinGameSubmit}>
        <DialogTitle>{strings.JoinGame}</DialogTitle>
        <DialogContent>
          <Stack spacing={2}>
            <DialogContentText>{strings.JoinGameDialog}</DialogContentText>
            <TextField
              label={strings.GameCode}
              required
              inputProps={{ inputMode: 'numeric' }}
              value={gameCode}
              onChange={handleCodeChange}
              autoFocus
              error={!!gameCodeError}
              helperText={gameCodeError}
              />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setJoinGameOpen(false)}>{strings.Cancel}</Button>
          <Button onClick={handleJoinGameSubmit}>{strings.JoinGame}</Button>
          <input type="submit" hidden />
        </DialogActions>
      </form>
    </Dialog>
    <Dialog
      open={rejoinGameOpen}
      onClose={() => setRejoinGameOpen(false)}
    >
      <form onSubmit={handleRejoinGameSubmit}>
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
          <Button onClick={() => setRejoinGameOpen(false)}>{strings.Cancel}</Button>
          <Button onClick={handleRejoinGameSubmit}>{strings.RejoinGame}</Button>
          <input type="submit" hidden />
        </DialogActions>
      </form>
    </Dialog>
  </Paper>
};
