import { Game } from '../models/game.model';
import { computeStatistics } from './statistics';

function makeGame(partial: Partial<Game>): Game {
  return {
    id: partial.id ?? Math.random().toString(36),
    title: partial.title ?? 'Игра',
    genre: partial.genre ?? 'RPG',
    platform: partial.platform ?? 'PC',
    status: partial.status ?? 'in-progress',
    rating: partial.rating,
    addedAt: partial.addedAt ?? '2024-01-01T00:00:00.000Z',
    updatedAt: partial.updatedAt ?? '2024-01-01T00:00:00.000Z',
  };
}

describe('computeStatistics', () => {
  it('считает показатели и доли по играм', () => {
    const games: Game[] = [
      makeGame({ status: 'completed', genre: 'RPG', platform: 'PC', rating: 10 }),
      makeGame({ status: 'completed', genre: 'RPG', platform: 'PC', rating: 8 }),
      makeGame({ status: 'in-progress', genre: 'Action', platform: 'PC' }),
      makeGame({ status: 'want-to-play', genre: 'Action', platform: 'PlayStation 5' }),
      makeGame({ status: 'dropped', genre: 'Simulation', platform: 'Nintendo Switch', rating: 6 }),
    ];

    const stats = computeStatistics(games);
    expect(stats.total).toBe(5);
    expect(stats.completed).toBe(2);
    expect(stats.inProgress).toBe(1);
    expect(stats.wantToPlay).toBe(1);
    expect(stats.dropped).toBe(1);
    expect(stats.completionRate).toBe(40);
    expect(stats.averageRating).toBe(8);
    // Action и RPG обе имеют count 2; вторичная сортировка по имени → Action первая.
    expect(stats.byGenre[0]).toEqual({ genre: 'Action', count: 2 });
    expect(stats.byPlatform[0]).toEqual({ platform: 'PC', count: 3 });
  });

  it('корректно обрабатывает пустую библиотеку', () => {
    const stats = computeStatistics([]);
    expect(stats.total).toBe(0);
    expect(stats.completionRate).toBe(0);
    expect(stats.averageRating).toBeNull();
    expect(stats.byGenre).toEqual([]);
  });
});
