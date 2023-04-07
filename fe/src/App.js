import {
  useEffect,
  useState,
} from 'react';
import {
  Backdrop,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Snackbar,
} from '@mui/material';

import {
  useAppState,
} from './AppContext';

import {
  Setup,
  GameStarted,
  GameNotStarted,
} from './Views';

export default function App() {
  const appState = useAppState();

  const { client, status, snackbar, clientState, strings } = appState;

  const [snackbarOpen, setSnackbarOpen] = useState(false);
  useEffect(() => {
    if (snackbar) setSnackbarOpen(true);
  }, [snackbar]);

  const [reconnectOpen, setReconnectOpen] = useState(false);
  useEffect(() => {
    if (clientState === 'disconnected') setReconnectOpen(true);
    setConnecting(clientState === 'connecting');
  }, [clientState]);

  const handleReconnect = async () => {
    try {
      await client.connect();
      setReconnectOpen(false);
    } catch (error) {
      console.error(error);
    }
  };

  const [connecting, setConnecting] = useState(false);

  let main;

  switch (status) {
    case 'Setup':
      main = <Setup />;
      break;
    case 'NotStarted':
      main = <GameNotStarted />;
      break;
    case 'Started':
      main = <GameStarted />;
      break;
    default:
      main = null;
      break;
  }

  return <>
    {main}
    <Snackbar
      open={snackbarOpen}
      autoHideDuration={5000}
      onClose={() => setSnackbarOpen(false)}
      message={snackbar}
    />
    <Dialog open={reconnectOpen}>
      <DialogTitle>{strings.Disconnected}</DialogTitle>
      <DialogContent>
        <DialogActions>
          <Button onClick={handleReconnect}>{strings.Reconnect}</Button>
        </DialogActions>
      </DialogContent>
    </Dialog>
    <Backdrop open={connecting} sx={{ color: '#fff', zIndex: 1000000 }}><CircularProgress  /></Backdrop>
  </>
};
