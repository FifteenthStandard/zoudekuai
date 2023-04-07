import React, {
  useState,
} from 'react';
import {
  Stack,
  Paper,
  Typography,
} from '@mui/material';

import {
  getInitialValue,
  useAppState,
} from '../../AppContext';

import Form from './Form';
import JoinGameDialog from './JoinGameDialog';
import Lang from './Lang';
import RejoinGameDialog from './RejoinGameDialog';

export default function Setup() {
  const { strings } = useAppState();

  const [name, setName] = useState(getInitialValue('name', ''));
  const [joinGameOpen, setJoinGameOpen] = useState(false);
  const storedGameCode = getInitialValue('gameCode', '');
  const [rejoinGameOpen, setRejoinGameOpen] = useState(!!storedGameCode);

  const center = { position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)' };

  return <Paper sx={{ width: '80vw', margin: 'auto', ...center }}>
    <Stack spacing={2} padding={2}>
      <Typography variant="h3" textAlign="center">{strings.Title}</Typography>
      <Lang />
      <Form {...{ name, setName, setJoinGameOpen }} />
    </Stack>
    <JoinGameDialog {...{ open: joinGameOpen, setOpen: setJoinGameOpen, name }} />
    <RejoinGameDialog {...{ open: rejoinGameOpen, setOpen: setRejoinGameOpen, name, storedGameCode }} />
  </Paper>;
};
