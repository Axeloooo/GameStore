import { GameSummary } from './GameSummary';

export interface GamesPage {
  totalPages: number;
  data: GameSummary[];
}