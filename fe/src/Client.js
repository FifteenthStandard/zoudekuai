import {
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

const baseUrl = 'http://localhost:7071/api';
const uuid = ('0000000000' + Math.floor(Math.random() * Math.pow(16, 10)).toString(16)).slice(-10);

export default class Client {
  constructor(dispatch) {
    this.dispatch = dispatch;

    this.connection = new HubConnectionBuilder()
      .withUrl(baseUrl)
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.start();
    this.connection.on('gameUpdate', function (game, message, ...args) {
      if (message) dispatch({ type: 'snackbar', message, args });
      dispatch({ type: 'gameUpdate', game: JSON.parse(game) });
    });
    this.connection.on('roundUpdate', function (round, message, ...args) {
      if (message) dispatch({ type: 'snackbar', message, args });
      dispatch({ type: 'roundUpdate', round: JSON.parse(round) });
    });
    this.connection.on('handUpdate', function (hand) {
      dispatch({ type: 'handUpdate', hand: JSON.parse(hand) });
    });
  }

  async register(name) {
    await this.post('/register', { name, connectionId: this.connection.connectionId });
  }

  async newGame(name) {
    await this.register(name);
    await this.post('/new-game');
  }

  async joinGame(name, gameCode) {
    await this.register(name);
    await this.post('/join-game', { gameCode });
  }

  async startGame(gameCode) {
    await this.post('/start-game', { gameCode });
  }

  async post(path, body) {
    body = JSON.stringify(body);
    const resp = await fetch(`${baseUrl}${path}`, { body, method: 'POST', headers: { 'Authorization': `Basic ${uuid}` } });
    try {
      return await resp.json();
    } catch {
    }
  }
}
