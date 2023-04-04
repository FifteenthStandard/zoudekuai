import {
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

const baseUrl = 'http://localhost:7071/api';

export default class Client {
  constructor(uuid, dispatch) {
    this.uuid = uuid;
    this.dispatch = dispatch;

    this.connect();
  }

  async connect() {
    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(baseUrl)
        .configureLogging(LogLevel.Warning)
        .build();
      await this.connection.start();
    } catch (error) {
      console.error(`Connection to server failed: ${error}`)
      this.dispatch({ type: 'connectionUpdate', state: 'disconnected' });
      return;
    }

    this.connection.onclose(error => {
      console.error(`Connection to server lost: ${error}`);
      this.dispatch({ type: 'disconnected' });
    })

    this.connection.on('rejoin', (game, message, ...args) => {
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'rejoin', game: JSON.parse(game) });
    });
    this.connection.on('gameUpdate', (game, message, ...args) => {
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'gameUpdate', game: JSON.parse(game) });
    });
    this.connection.on('roundUpdate', (round, message, ...args) => {
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'roundUpdate', round: JSON.parse(round) });
    });
    this.connection.on('handUpdate', (hand) => {
      this.dispatch({ type: 'handUpdate', hand: JSON.parse(hand) });
    });

    this.dispatch({ type: 'connectionUpdate', state: 'connected' });
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

  async startRound(gameCode) {
    await this.post('/start-round', { gameCode });
  }

  async playCards(gameCode, cardIndexes) {
    await this.post('/play-cards', { gameCode, cardIndexes });
  }

  async post(path, body) {
    body = JSON.stringify(body);
    for (let attempt = 0; attempt < 5; attempt++) {
      const resp = await fetch(`${baseUrl}${path}`, { body, method: 'POST', headers: { 'Authorization': `Basic ${this.uuid}` } });
      if (resp.ok) {
        try {
          return await resp.json();
        } catch {
          return;
        }
      } else if (resp.status < 500) {
        throw new Error(`${resp.status} ${resp.statusText}: ${await resp.text()}`);
      } else {
        console.warn(`${resp.status} ${resp.statusText}: ${await resp.text()}`);
        await new Promise(resolve => setTimeout(resolve, 5000));
      }
    }
    throw new Error('Request failed after 5 attempts');
  }
}
