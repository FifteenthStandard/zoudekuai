import {
  useEffect,
  useState,
} from 'react';
import {
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

  const [snackbarOpen, setSnackbarOpen] = useState(false);
  useEffect(() => {
    if (appState.snackbar) setSnackbarOpen(true);
  }, [appState.snackbar]);

  let main;

  switch (appState.status) {
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
      message={appState.snackbar}
    />
  </>
};
