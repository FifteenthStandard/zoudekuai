import {
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

const baseUrl = 'https://zoudekuai.azurewebsites.net/api';

export default class Client {
  constructor(uuid, dispatch) {
    this.uuid = uuid;
    this.dispatch = dispatch;

    this.connect();
  }

  async connect() {
    this.post('/warmup');

    try {
      this.connection = new HubConnectionBuilder()
        .withUrl(baseUrl)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();
      await this.connection.start();
    } catch (error) {
      console.error(`Connection to server failed: ${error}`)
      setTimeout(() => this.connect(), 1000);
      return;
    }

    this.connection.onreconnecting(error => {
      console.error(`Connection to server lost: ${error}`);
      this.dispatch({ type: 'connectionUpdate', state: 'reconnecting' });
    });

    this.connection.onreconnected(() => {
      console.debug('Connection to server reconnected');
      this.dispatch({ type: 'connectionUpdate', state: 'connected' });
    });

    this.connection.onclose(error => {
      console.error(`Connection to server lost: ${error}`);
      this.dispatch({ type: 'connectionUpdate', state: 'disconnected' });
    })

    this.connection.on('gameJoin', (game, message, ...args) => {
      console.debug('gameJoin', game);
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'gameJoin', game: JSON.parse(game) });
    });
    this.connection.on('gameUpdate', (game, message, ...args) => {
      console.debug('gameUpdate', game);
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'gameUpdate', game: JSON.parse(game) });
    });
    this.connection.on('roundUpdate', (round, message, ...args) => {
      console.debug('roundUpdate', round);
      if (message) this.dispatch({ type: 'snackbar', message, args });
      this.dispatch({ type: 'roundUpdate', round: JSON.parse(round) });
    });
    this.connection.on('handUpdate', (hand) => {
      console.debug('handUpdate', hand);
      this.dispatch({ type: 'handUpdate', hand: JSON.parse(hand) });
    });

    this.dispatch({ type: 'connectionUpdate', state: 'connected' });
  }

  async register(name) {
    while (this.connection.state === 'Connecting') {
      console.debug('Connection still connecting in attempt to register. Waiting...');
      await this.delay();
    }
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
        await this.delay();
      }
    }
    throw new Error('Request failed after 5 attempts');
  }

  async delay() {
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
}
