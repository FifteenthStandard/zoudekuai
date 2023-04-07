import {
  useEffect,
  useRef,
} from 'react';
import {
  Paper,
  Stack,
  Typography,
} from '@mui/material';

import {
  useAppState,
} from '../../AppContext';
import {
  CardChars,
} from '../Shared';

export default function DiscardPile() {
  const { round } = useAppState();

  const discardRef = useRef(null);
  useEffect(function () {
    if (round.discard.length) {
      discardRef.current.lastChild.scrollIntoView();
      window.scroll(0, 0); // revert entire window scrolling
    }
  }, [round.discard]);

  return <Stack ref={discardRef} direction="column" spacing={1}
    sx={{ position: 'absolute', top: 300, height: 'calc(100vh - 450px)', width: '100%', overflowY: 'scroll', display: 'flex', alignItems: 'center' }}
  >
    {
      round.discard.map((cards, index) =>
        <Paper key={index} sx={{ width: 'fit-content' }}>
          <Stack direction="row" paddingInline={1}>
            {
              cards.map(({ suit, value }) =>
                <Typography
                  key={value}
                  variant="h1"
                  color={suit % 2 ? '#000' : '#f00'}
                >
                  {CardChars[value]}
                </Typography>
              )
            }
          </Stack>
        </Paper>
      )
    }
  </Stack>;
};
