import {
  createContext,
  useContext,
  useReducer,
} from 'react';
import Client from './Client';
import getStrings from './I18n';

const AppContext = createContext(null);
const AppDispatchContext = createContext(null);

export function getInitialValue(param, defaultValue, useHash) {
  let value = (useHash && window.location.hash.substring(1)) || new URLSearchParams(window.location.search).get(param) || localStorage.getItem(`zoudekuai:${param}`);
  if (!value) {
    value = typeof(defaultValue) === 'function' ? defaultValue() : defaultValue;
    localStorage.setItem(`zoudekuai:${param}`, value);
  }
  return value;
}

const uuid = getInitialValue('uuid', () => ('0000000000' + Math.floor(Math.random() * Math.pow(16, 10)).toString(16)).slice(-10));
const lang = getInitialValue('lang', navigator.language.startsWith('zh') ? 'zh' : 'en');

let client;

export function AppProvider({ children }) {
  const initialState = {
    // Cannot access 'dispatch' before initialization
    snackbar: null,
    client: client || (client = new Client(uuid, action => dispatch(action))),
    clientState: null,
    loading: false,
    status: 'Setup',
    lang,
    name: null,
    strings: getStrings(lang),
    isHost: null,
    game: {
      gameCode: null,
      host: null,
      status: null,
      players: [],
    },
    round: {
      roundNumber: null,
      status: null,
      freePlay: null,
      stealChance: null,
      firstPlayContinuation: null,
      players: [],
      discard: [],
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
      case 'loading':
        return {
          ...state,
          loading: action.value,
        }
      case 'connectionUpdate':
        if (action.state === 'connected' && state.game.gameCode) {
          console.debug('Rejoining game after connection lost');
          client.joinGame(state.name, state.game.gameCode);
        }
        return {
          ...state,
          clientState: action.state,
        };
      case 'setLang':
        const lang = action.lang || state.lang;
        localStorage.setItem('zoudekuai:lang', lang);
        return {
          ...state,
          lang,
          strings: getStrings(lang),
        };
      case 'gameJoin':
        localStorage.setItem('zoudekuai:gameCode', action.game.gameCode);
        return {
          ...state,
          status: action.game.status,
          isHost: action.game.host,
          game: action.game,
        };
      case 'gameUpdate':
        localStorage.setItem('zoudekuai:gameCode', action.game.gameCode);
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
