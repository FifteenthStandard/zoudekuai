import {
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material';
import {
  useAppDispatch,
  useAppState,
} from "../../AppContext";

export default function Lang() {
  const appDispatch = useAppDispatch();
  const { lang } = useAppState();

  return <ToggleButtonGroup
    exclusive
    value={lang}
    onChange={(_, lang) => appDispatch({ type: 'setLang', lang })}
    color="primary"
    fullWidth
  >
    <ToggleButton value="zh">中文</ToggleButton>
    <ToggleButton value="en">EN</ToggleButton>
  </ToggleButtonGroup>;
};
