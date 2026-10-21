import {
  Game,
  GameStatus,
  GenreCount,
  LibraryStatistics,
  PlatformCount,
  StatusCount,
} from '../models';
import { GAME_STATUSES, GAME_STATUS_META } from '../constants/game-status';

function countBy(values: string[]): Map<string, number> {
  const map = new Map<string, number>();
  for (const value of values) {
    map.set(value, (map.get(value) ?? 0) + 1);
  }
  return map;
}

function toSortedCounts(map: Map<string, number>): { key: string; count: number }[] {
  return [...map.entries()]
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count || a.key.localeCompare(b.key));
}

/** Считает сводную статистику по библиотеке игр. */
export function computeStatistics(games: readonly Game[]): LibraryStatistics {
  const total = games.length;
  const statusCounts = countBy(games.map((game) => game.status)) as Map<GameStatus, number>;
  const byStatus: StatusCount[] = GAME_STATUSES.map((status) => ({
    status,
    label: GAME_STATUS_META[status].label,
    count: statusCounts.get(status) ?? 0,
  }));

  const byGenre: GenreCount[] = toSortedCounts(
    countBy(games.map((game) => game.genre)),
  ).map(({ key, count }) => ({ genre: key, count }));

  const byPlatform: PlatformCount[] = toSortedCounts(
    countBy(games.map((game) => game.platform)),
  ).map(({ key, count }) => ({ platform: key, count }));

  const rated = games.filter((game) => typeof game.rating === 'number');
  const averageRating =
    rated.length > 0
      ? Math.round((rated.reduce((sum, game) => sum + (game.rating ?? 0), 0) / rated.length) * 10) / 10
      : null;

  const completed = statusCounts.get('completed') ?? 0;
  const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0;

  return {
    total,
    completed,
    inProgress: statusCounts.get('in-progress') ?? 0,
    wantToPlay: statusCounts.get('want-to-play') ?? 0,
    dropped: statusCounts.get('dropped') ?? 0,
    completionRate,
    averageRating,
    byStatus,
    byGenre,
    byPlatform,
  };
}

/** Возвращает долю значения в общем объёме (0..100). */
export function sharePercent(count: number, total: number): number {
  return total > 0 ? Math.round((count / total) * 100) : 0;
}
