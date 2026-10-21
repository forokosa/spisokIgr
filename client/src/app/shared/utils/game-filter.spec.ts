import { Game } from '../models/game.model';
import { filterGames, matchesQuery } from './game-filter';

function makeGame(partial: Partial<Game>): Game {
  return {
    id: partial.id ?? Math.random().toString(36),
    title: partial.title ?? 'Игра',
    genre: partial.genre ?? 'RPG',
    platform: partial.platform ?? 'PC',
    status: partial.status ?? 'in-progress',
    addedAt: partial.addedAt ?? '2024-01-01T00:00:00.000Z',
    updatedAt: partial.updatedAt ?? '2024-01-01T00:00:00.000Z',
  };
}

describe('game filter', () => {
  const games = [
    makeGame({ id: 'a', title: 'The Witcher 3', genre: 'RPG', platform: 'PC', status: 'completed', updatedAt: '2024-01-01T00:00:00.000Z' }),
    makeGame({ id: 'b', title: 'Cyberpunk 2077', genre: 'RPG', platform: 'PC', status: 'in-progress', updatedAt: '2024-03-01T00:00:00.000Z' }),
    makeGame({ id: 'c', title: 'Stardew Valley', genre: 'Simulation', platform: 'Nintendo Switch', status: 'dropped', updatedAt: '2024-02-01T00:00:00.000Z' }),
  ];

  it('сортирует по дате обновления (сначала свежие)', () => {
    const result = filterGames(games);
    expect(result.map((game) => game.id)).toEqual(['b', 'c', 'a']);
  });

  it('ищет по названию, жанру и платформе без учёта регистра', () => {
    expect(matchesQuery(games[0], { search: 'WITCHER' })).toBeTrue();
    expect(matchesQuery(games[0], { search: 'rpg' })).toBeTrue();
    expect(matchesQuery(games[2], { search: 'switch' })).toBeTrue();
    expect(matchesQuery(games[0], { search: 'stardew' })).toBeFalse();
  });

  it('комбинирует поиск и статус', () => {
    const result = filterGames(games, { search: 'rpg', status: 'in-progress' });
    expect(result.length).toBe(1);
    expect(result[0].id).toBe('b');
  });
});
