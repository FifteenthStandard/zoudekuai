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

  const strings = appState.strings;

  const [submitted, setSubmitted] = useState(false);

  const [name, setName] = useState(getInitialValue('name', ''));
  const [nameError, setNameError] = useState('');

  const handleNewGame = () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    appDispatch({ type: 'newGame', name });
    setSubmitted(true);
  };

  const [joinGameOpen, setJoinGameOpen] = useState(false);
  const handleJoinGameOpen = () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    setJoinGameOpen(true);
  };
  const handleJoinGameClose = () => setJoinGameOpen(false);

  const [gameCode, setGameCode] = useState('');
  const [gameCodeError, setGameCodeError] = useState('');
  const handleJoinGameSubmit = (ev) => {
    ev.preventDefault();
    if (!gameCode) {
      setGameCodeError(strings.GameCodeRequired);
      return;
    }
    appDispatch({ type: 'joinGame', name, gameCode });
    setSubmitted(true);
  };

  const center = {position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)'};

  return <>
    <Backdrop open={submitted} sx={{ color: '#fff', zIndex: 1000000 }}><CircularProgress  /></Backdrop>
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
          <ToggleButton value="cn">中文</ToggleButton>
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
        onClose={handleJoinGameClose}
      >
        <form onSubmit={handleJoinGameSubmit}>
          <DialogTitle>{strings.JoinGame}</DialogTitle>
          <DialogContent>
            <Stack spacing={2}>
              <DialogContentText>{strings.JoinGameDialog}</DialogContentText>
              <TextField
                label={strings.GameCode}
                required
                value={gameCode}
                onChange={ev => setGameCode(ev.target.value) || setGameCodeError('')}
                autoFocus
                error={!!gameCodeError}
                helperText={gameCodeError}
                />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={handleJoinGameClose}>{strings.Cancel}</Button>
            <Button onClick={handleJoinGameSubmit}>{strings.JoinGame}</Button>
            <input type="submit" hidden />
          </DialogActions>
        </form>
      </Dialog>
    </Paper>
  </>
};
