import { Game, GameQuery } from '../models';

/** Проверяет, соответствует ли игра поисковому запросу и статусу. */
export function matchesQuery(game: Game, query: GameQuery): boolean {
  const status = query.status ?? 'all';
  if (status !== 'all' && game.status !== status) {
    return false;
  }
  const needle = (query.search ?? '').trim().toLowerCase();
  if (needle.length === 0) {
    return true;
  }
  return (
    game.title.toLowerCase().includes(needle) ||
    game.genre.toLowerCase().includes(needle) ||
    game.platform.toLowerCase().includes(needle)
  );
}

/**
 * Единая логика фильтрации и сортировки библиотеки.
 * Используется и mock-обработчиком (сервер), и клиентскими computed-сигналами.
 */
export function filterGames(games: readonly Game[], query: GameQuery = {}): Game[] {
  return games
    .filter((game) => matchesQuery(game, query))
    .slice()
    .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
}
