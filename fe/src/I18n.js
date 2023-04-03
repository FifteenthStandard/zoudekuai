const Strings = {
  'cn': {
    Cancel: 'Cancel 🇨🇳',
    CardsPlayed: name => `${name} took their turn 🇨🇳`,
    GameCode: 'Game Code 🇨🇳',
    GameCodeRequired: 'Game Code is required 🇨🇳',
    JoinGame: 'JG 🇨🇳',
    JoinGameDialog: 'Enter the game code 🇨🇳',
    Name: 'Name 🇨🇳',
    NameRequired: 'Name is required 🇨🇳',
    NewGame: 'NG 🇨🇳',
    PlayerJoined: name => `${name} has joined the game! 🇨🇳`,
    Players: 'Players 🇨🇳',
    RoundStarted: 'Round started! 🇨🇳',
    StartGame: 'Start Game 🇨🇳',
    Title: 'ZDK 🇨🇳',
    WaitingForHost: 'Waiting for host 🇨🇳',
    WaitingForPlayers: 'Waiting for players 🇨🇳',
  },
  'en': {
    Cancel: 'Cancel',
    CardsPlayed: name => `${name} took their turn`,
    GameCode: 'Game Code',
    GameCodeRequired: 'Game Code is required',
    JoinGame: 'Join Game',
    JoinGameDialog: 'Enter the game code',
    Name: 'Name',
    NameRequired: 'Name is required',
    NewGame: 'New Game',
    PlayerJoined: name => `${name} has joined the game!`,
    Players: 'Players',
    RoundStarted: 'Round started!',
    StartGame: 'Start Game',
    Title: 'Zoudekuai',
    WaitingForHost: 'Waiting for host',
    WaitingForPlayers: 'Waiting for players',
  },
};

export default function getStrings(lang) {
  return Strings[lang];
};
