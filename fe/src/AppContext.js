import {
  createContext,
  useContext,
  useReducer,
} from 'react';
import Client from './Client';
import getStrings from './I18n';

const AppContext = createContext(null);
const AppDispatchContext = createContext(null);

export function getInitialValue(param, defaultValue) {
  let value = new URLSearchParams(window.location.search).get(param) || localStorage.getItem(`zoudekuai:${param}`);
  if (!value) {
    value = typeof(defaultValue) === 'function' ? defaultValue() : defaultValue;
    localStorage.setItem(`zoudekuai:${param}`, value);
  }
  return value;
}

const uuid = getInitialValue('uuid', () => ('0000000000' + Math.floor(Math.random() * Math.pow(16, 10)).toString(16)).slice(-10));
const lang = getInitialValue('lang', 'cn');

let client;

export function AppProvider({ children }) {
  const initialState = {
    // Cannot access 'dispatch' before initialization
    snackbar: null,
    client: client || (client = new Client(uuid, action => dispatch(action))),
    clientState: null,
    status: 'Setup',
    lang,
    strings: getStrings(lang),
    isHost: null,
    game: {
      gameCode: null,
      host: null,
      status: null,
      players: [],
    },
    round: {
      status: null,
      roundNumber: null,
      players: [],
    },
    hand: {
      cards: [],
    },
  };

  const reducer = function (state, action) {
    switch (action.type) {
      case 'snackbar':
        let snackbar = state.strings[action.message];
        if (action.args.length) snackbar = snackbar(...action.args);
        return {
          ...state,
          snackbar,
        };
      case 'connectionUpdate':
        return {
          ...state,
          clientState: action.state,
        };
      case 'reconnect':
        state.client.connect();
        return {
          ...state,
          clientState: 'connecting',
        };
      case 'setLang':
        const lang = action.lang || state.lang;
        localStorage.setItem('zoudekuai:lang', lang);
        return {
          ...state,
          lang,
          strings: getStrings(lang),
        };
      case 'newGame':
        localStorage.setItem('zoudekuai:name', action.name);
        state.client.newGame(action.name);
        return {
          ...state,
          isHost: true,
        };
      case 'joinGame':
        localStorage.setItem('zoudekuai:name', action.name);
        state.client.joinGame(action.name, action.gameCode);
        return {
          ...state,
          isHost: false,
        };
      case 'startGame':
        state.client.startGame(state.game.gameCode);
        return state;
      case 'playCards':
        state.client.playCards(state.game.gameCode, action.cardIndexes);
        return state;
      case 'rejoin':
        return {
          ...state,
          status: action.game.status,
          isHost: action.game.host,
          game: action.game,
        };
      case 'gameUpdate':
        return {
          ...state,
          status: action.game.status,
          game: action.game,
        };
      case 'roundUpdate':
        return {
          ...state,
          round: action.round,
        };
      case 'handUpdate':
        return {
          ...state,
          hand: action.hand,
        };
      default:
        return state;
    }
  };

  const [state, dispatch] = useReducer(reducer, initialState);

  return <AppContext.Provider value={state}>
    <AppDispatchContext.Provider value={dispatch}>
      {children}
    </AppDispatchContext.Provider>
  </AppContext.Provider>
};

export function useAppState() { return useContext(AppContext); };
export function useAppDispatch() { return useContext(AppDispatchContext); };
