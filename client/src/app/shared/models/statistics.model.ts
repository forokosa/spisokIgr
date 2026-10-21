import { GameStatus } from './game.model';

/** Количество игр по конкретному статусу. */
export interface StatusCount {
  status: GameStatus;
  label: string;
  count: number;
}

/** Количество игр по жанру. */
export interface GenreCount {
  genre: string;
  count: number;
}

/** Количество игр по платформе. */
export interface PlatformCount {
  platform: string;
  count: number;
}

/** Сводная статистика библиотеки. */
export interface LibraryStatistics {
  total: number;
  completed: number;
  inProgress: number;
  wantToPlay: number;
  dropped: number;
  /** Доля пройденных игр, 0..100. */
  completionRate: number;
  /** Средний рейтинг по играм, где он задан. */
  averageRating: number | null;
  byStatus: StatusCount[];
  byGenre: GenreCount[];
  byPlatform: PlatformCount[];
}
