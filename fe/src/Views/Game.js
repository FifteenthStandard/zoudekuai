// import React, {
//   useEffect,
//   useState,
// } from 'react';
// import {
//   Backdrop,
//   CircularProgress,
//   Paper,
//   Stack,
//   Typography,
// } from '@mui/material';
// import {
//   Report,
// } from '@mui/icons-material';
// import {
//   HubConnectionBuilder,
//   LogLevel,
// } from '@microsoft/signalr';

// const CardChars = [
//   '🃁','🃂','🃃','🃄','🃅','🃆','🃇','🃈','🃉','🃊','🃋','🃍','🃎',
//   '🃑','🃒','🃓','🃔','🃕','🃖','🃗','🃘','🃙','🃚','🃛','🃝','🃞',
//   '🂱','🂲','🂳','🂴','🂵','🂶','🂷','🂸','🂹','🂺','🂻','🂽','🂾',
//   '🂡','🂢','🂣','🂤','🂥','🂦','🂧','🂨','🂩','🂪','🂫','🂭','🂮',
// ];

// export default function Game({ uuid, name, gameCode, setGameCode }) {
//   const [connected, setConnected] = useState(false);
//   const [game, setGame] = useState({});

//   useEffect(() => {
//     let abandoned = false;
//     async function handle() {
//       const baseUrl = 'http://localhost:7071/api';

//       async function get(path) {
//         const resp = await fetch(`${baseUrl}${path}`, { headers: { 'Authorization': `Basic ${uuid}` } });
//         const json = await resp.json();
//         return json;
//       }

//       async function post(path, body) {
//         body = JSON.stringify(body);
//         const resp = await fetch(`${baseUrl}${path}`, { body, method: 'POST', headers: { 'Authorization': `Basic ${uuid}` } });
//         try {
//           const json = await resp.json();
//           return json;
//         } catch {
//         }
//       }

//       const connection = new HubConnectionBuilder()
//         .withUrl(baseUrl)
//         .configureLogging(LogLevel.Information)
//         .build();

//       if (abandoned) return;

//       await connection.start();
//       const { connectionId } = connection;

//       if (abandoned) return;

//       await post('/register', { name, connectionId });
//       setConnected(true);

//       connection.on('gameUpdate', function (message, data) {
//         console.log('gameUpdate', message, data);
//         const game = JSON.parse(data);
//         setGame(game);
//       });

//       if (gameCode) {
//         await post('/join-game', { gameCode })
//       } else {
//         setGameCode((await post('/new-game', { uuid })).gameCode);
//       }
//     }
//     handle();
//     return () => { abandoned = true; };
//   }, []);

//   return <>
//     <Backdrop open={!connected}>
//       <CircularProgress />
//     </Backdrop>
//     {
//       game.status === 'NotStarted' && <>
//         <p onClick={() => navigator.clipboard.writeText(game.gameCode)}>{game.gameCode}</p>
//         <ul>
//           {
//             game.players.map((player, ind) => <li key={`${ind}-${player}`}>{player}</li>)
//           }
//         </ul>
//         <p>Waiting for the host...</p>
//       </>
//     }
//     {
//       game.status === 'Started' && <>
//         <Paper square={true} sx={{ position: 'absolute', top: 0, width: '100vw' }}>
//           <Typography onClick={() => navigator.clipboard.writeText(gameCode)}>{gameCode}</Typography>
//         </Paper>
//         {game.players.map(({ name, cards, stole, position }, ind) =>
//           <Paper key={ind} sx={{ position: 'absolute', width: '40vw', ...position }}>
//             <Stack padding={1} direction="row" sx={{ display: 'flex', justifyContent: 'space-around', alignItems: 'center' }}>
//               <Typography fontSize={25}>
//                 {name}
//               </Typography>
//               {stole && <Report />}
//               <Typography fontSize={25}>
//                 {'🂠'.repeat(cards)}
//               </Typography>
//             </Stack>
//           </Paper>
//         )}
//         <Stack
//           direction="row"
//           spacing={2}
//           sx={{ position: 'absolute', width: '100vw', left: 0, top: 400, overflowX: 'scroll' }}>
//           {game.discard.map(({ card, value }) => <Paper key={value}><Typography fontSize={150} sx={{ lineHeight: '1em' }}>{card}</Typography></Paper>)}
//         </Stack>
//         <Paper square={true} sx={{ position: 'absolute', bottom: 0, width: '100vw' }}>
//           <Stack direction="row" sx={{ display: 'flex', justifyContent: 'space-around' }}>
//             {game.hand.map(({ card, value }) => <Typography key={value} variant="h1">{card}</Typography>)}
//           </Stack>
//         </Paper>
//       </>
//     }
//   </>;
// };
