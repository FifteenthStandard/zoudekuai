import React, {
  useState,
} from 'react';
import {
  Backdrop,
  Button,
  ButtonGroup,
  CircularProgress,
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

  const { strings, client } = appState;

  const [name, setName] = useState(getInitialValue('name', ''));
  const [nameError, setNameError] = useState('');

  const [submitted, setSubmitted] = useState(false);

  const handleNewGame = async () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    setSubmitted(true);
    try {
      await client.newGame(name);
    } catch (error) {
      alert(error);
      setSubmitted(false);
    }
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

  const handleJoinGameSubmit = async (ev) => {
    ev.preventDefault();
    if (!gameCode) {
      setGameCodeError(strings.GameCodeRequired);
      return;
    }
    setSubmitted(true);
    try {
      await client.joinGame(name, gameCode);
    } catch (error) {
      alert(error);
      setSubmitted(false);
    }
  };

  const storedGameCode = getInitialValue('gameCode', '');
  const [rejoinGameOpen, setRejoinGameOpen] = useState(!!storedGameCode);

  const handleRejoinGameSubmit = async (ev) => {
    ev.preventDefault();
    setSubmitted(true);
    try {
      await client.joinGame(name, storedGameCode);
    } catch (error) {
      console.error(error);
    }
  };

  const center = {position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)'};

  return <>
    <Backdrop open={submitted} sx={{ color: '#fff', zIndex: 1000000 }}><CircularProgress /></Backdrop>
    <Paper sx={{ width: '80vw', margin: 'auto', ...center }}>
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
  </>
};
