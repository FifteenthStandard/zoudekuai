import {
  useState,
} from 'react';
import {
  Button,
  ButtonGroup,
  TextField,
} from '@mui/material';

import {
  useAppState,
} from '../../AppContext';

export default function Form({ name, setName, setJoinGameOpen }) {
  const { client, strings } = useAppState();

  const [nameError, setNameError] = useState('');

  const handleNewGame = async () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    try {
      await client.newGame(name);
    } catch (error) {
      alert(error);
    }
  };

  const handleJoinGameOpen = () => {
    if (!name) {
      setNameError(strings.NameRequired);
      return;
    }
    setJoinGameOpen(true);
  };

  return <form>
    <TextField
      label={strings.Name}
      required
      value={name}
      onChange={ev => setName(ev.target.value) || setNameError('')}
      error={!!nameError}
      helperText={nameError}
      fullWidth
      autoFocus
      margin="normal"
    />
    <ButtonGroup fullWidth variant="contained">
      <Button onClick={handleNewGame}>
        {strings.NewGame}
      </Button>
      <Button onClick={handleJoinGameOpen}>
        {strings.JoinGame}
      </Button>
    </ButtonGroup>
  </form>
};
