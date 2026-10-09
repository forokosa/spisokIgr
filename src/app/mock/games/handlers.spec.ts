import { GameDraft } from '../../shared/models/game.model';
import { createSeedDatabase } from '../seed';
import { MockApiError } from '../mock-error';
import {
  changeGameStatusHandler,
  createGameHandler,
  deleteGameHandler,
  getGameHandler,
  listGamesHandler,
  updateGameHandler,
} from './handlers';

const DRAFT: GameDraft = {
  title: 'Новая игра',
  genre: 'RPG',
  platform: 'PC',
  status: 'want-to-play',
};

describe('games handlers', () => {
  it('возвращает начальный набор игр', () => {
    const db = createSeedDatabase();
    const games = listGamesHandler(db);
    expect(games.length).toBeGreaterThanOrEqual(5);
    expect(games.map((game) => game.title)).toContain('The Witcher 3: Wild Hunt');
  });

  it('фильтрует по статусу и поисковому запросу совместно', () => {
    const db = createSeedDatabase();
    const completed = listGamesHandler(db, { status: 'completed' });
    expect(completed.every((game) => game.status === 'completed')).toBeTrue();

    const cyber = listGamesHandler(db, { search: 'cyber', status: 'all' });
    expect(cyber.length).toBe(1);
    expect(cyber[0].title).toBe('Cyberpunk 2077');

    const cyberCompleted = listGamesHandler(db, { search: 'cyber', status: 'completed' });
    expect(cyberCompleted.length).toBe(0);
  });

  it('создаёт новую игру и делает её доступной по id', () => {
    const db = createSeedDatabase();
    const before = db.games.length;
    const created = createGameHandler(db, DRAFT);
    expect(created.id).toBeTruthy();
    expect(db.games.length).toBe(before + 1);
    expect(getGameHandler(db, created.id).title).toBe('Новая игра');
  });

  it('не допускает дубликат названия', () => {
    const db = createSeedDatabase();
    expect(() => createGameHandler(db, { ...DRAFT, title: 'The Witcher 3: Wild Hunt' })).toThrowError(
      MockApiError,
    );
  });

  it('меняет статус игры', () => {
    const db = createSeedDatabase();
    const target = db.games[0];
    const updated = changeGameStatusHandler(db, target.id, 'completed');
    expect(updated.status).toBe('completed');
  });

  it('обновляет поля игры', () => {
    const db = createSeedDatabase();
    const target = db.games[0];
    const updated = updateGameHandler(db, target.id, { title: 'Обновлённое название', rating: 8 });
    expect(updated.title).toBe('Обновлённое название');
    expect(updated.rating).toBe(8);
  });

  it('удаляет игру и выбрасывает ошибку для несуществующей', () => {
    const db = createSeedDatabase();
    const target = db.games[0];
    deleteGameHandler(db, target.id);
    expect(db.games.some((game) => game.id === target.id)).toBeFalse();
    expect(() => getGameHandler(db, 'missing')).toThrowError(MockApiError);
  });
});
