import { Game, GameDraft, GamePatch, GameQuery, GameStatus } from '../../shared/models';
import { filterGames } from '../../shared/utils/game-filter';
import { createId, nowIso } from '../ids';
import { MockApiError } from '../mock-error';
import { MockDatabase } from '../mock-db.model';

const VALID_STATUSES: readonly GameStatus[] = [
  'in-progress',
  'completed',
  'want-to-play',
  'dropped',
];

function findGame(db: MockDatabase, id: string): Game {
  const game = db.games.find((item) => item.id === id);
  if (!game) {
    throw new MockApiError('not-found', 'Игра не найдена', 404);
  }
  return game;
}

function validateDraft(db: MockDatabase, draft: GameDraft, currentId?: string): void {
  if (!draft.title || draft.title.trim().length < 2) {
    throw new MockApiError('validation', 'Название должно содержать минимум 2 символа');
  }
  if (!draft.genre) {
    throw new MockApiError('validation', 'Выберите жанр');
  }
  if (!draft.platform) {
    throw new MockApiError('validation', 'Выберите платформу');
  }
  if (!VALID_STATUSES.includes(draft.status)) {
    throw new MockApiError('validation', 'Некорректный статус игры');
  }
  if (draft.rating !== undefined && (draft.rating < 0 || draft.rating > 10)) {
    throw new MockApiError('validation', 'Рейтинг должен быть от 0 до 10');
  }
  if (draft.releaseYear !== undefined && (draft.releaseYear < 1970 || draft.releaseYear > 2100)) {
    throw new MockApiError('validation', 'Некорректный год выпуска');
  }
  const duplicate = db.games.find(
    (game) => game.title.trim().toLowerCase() === draft.title.trim().toLowerCase() && game.id !== currentId,
  );
  if (duplicate) {
    throw new MockApiError('conflict', 'Игра с таким названием уже есть в библиотеке', 409);
  }
}

/** Возвращает отфильтрованный и отсортированный список игр. */
export function listGamesHandler(db: MockDatabase, query: GameQuery = {}): Game[] {
  return filterGames(db.games, query).map((game) => ({ ...game }));
}

/** Возвращает одну игру по идентификатору. */
export function getGameHandler(db: MockDatabase, id: string): Game {
  return { ...findGame(db, id) };
}

/** Создаёт игру. */
export function createGameHandler(db: MockDatabase, draft: GameDraft): Game {
  validateDraft(db, draft);
  const timestamp = nowIso();
  const game: Game = {
    id: createId('game'),
    title: draft.title.trim(),
    genre: draft.genre,
    platform: draft.platform,
    status: draft.status,
    coverUrl: draft.coverUrl,
    description: draft.description?.trim(),
    rating: draft.rating,
    releaseYear: draft.releaseYear,
    addedAt: timestamp,
    updatedAt: timestamp,
  };
  db.games = [game, ...db.games];
  return { ...game };
}

/** Обновляет игру целиком по черновику. */
export function updateGameHandler(db: MockDatabase, id: string, patch: GamePatch): Game {
  const game = findGame(db, id);
  validateDraft(
    db,
    {
      title: patch.title ?? game.title,
      genre: patch.genre ?? game.genre,
      platform: patch.platform ?? game.platform,
      status: patch.status ?? game.status,
      rating: patch.rating ?? game.rating,
      releaseYear: patch.releaseYear ?? game.releaseYear,
    },
    id,
  );
  Object.assign(game, patch, {
    title: (patch.title ?? game.title).trim(),
    updatedAt: nowIso(),
  });
  return { ...game };
}

/** Меняет статус игры. */
export function changeGameStatusHandler(db: MockDatabase, id: string, status: GameStatus): Game {
  if (!VALID_STATUSES.includes(status)) {
    throw new MockApiError('validation', 'Некорректный статус игры');
  }
  const game = findGame(db, id);
  game.status = status;
  game.updatedAt = nowIso();
  return { ...game };
}

/** Удаляет игру из библиотеки. */
export function deleteGameHandler(db: MockDatabase, id: string): string {
  findGame(db, id);
  db.games = db.games.filter((game) => game.id !== id);
  return id;
}
