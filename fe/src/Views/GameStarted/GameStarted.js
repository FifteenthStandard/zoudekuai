import {
  useAppState,
} from '../../AppContext';
import {
  GameCodePanel,
} from '../Shared';
import Players from './Players';
import DiscardPile from './DiscardPile';
import Hand from './Hand';
import RoundFinished from './RoundFinished';

export default function GameStarted() {
  const { round: { status } } = useAppState();

  return <>
    <GameCodePanel />
    <Players />
    <DiscardPile />
    <Hand />
    {status === 'Finished' && <RoundFinished />}
  </>
}