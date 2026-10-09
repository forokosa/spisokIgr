import { Game, GameStatus, UserProfile } from '../shared/models';
import { MOCK_DB_STORAGE_KEY, MOCK_DB_VERSION, MockDatabase } from './mock-db.model';
import { createSeedDatabase } from './seed';

const VALID_STATUSES: readonly GameStatus[] = [
  'in-progress',
  'completed',
  'want-to-play',
  'dropped',
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isValidGame(value: unknown): value is Game {
  if (!isRecord(value)) {
    return false;
  }
  return (
    typeof value['id'] === 'string' &&
    typeof value['title'] === 'string' &&
    typeof value['genre'] === 'string' &&
    typeof value['platform'] === 'string' &&
    typeof value['status'] === 'string' &&
    VALID_STATUSES.includes(value['status'] as GameStatus) &&
    typeof value['addedAt'] === 'string' &&
    typeof value['updatedAt'] === 'string'
  );
}

function isValidProfile(value: unknown): value is UserProfile {
  if (!isRecord(value)) {
    return false;
  }
  return (
    typeof value['id'] === 'string' &&
    typeof value['name'] === 'string' &&
    typeof value['email'] === 'string'
  );
}

/** Проверяет, что данные из хранилища соответствуют ожидаемой структуре. */
export function isValidDatabase(value: unknown): value is MockDatabase {
  if (!isRecord(value)) {
    return false;
  }
  if (value['version'] !== MOCK_DB_VERSION) {
    return false;
  }
  if (!Array.isArray(value['games']) || !value['games'].every(isValidGame)) {
    return false;
  }
  return isValidProfile(value['profile']);
}

function storageAvailable(): boolean {
  try {
    return typeof localStorage !== 'undefined';
  } catch {
    return false;
  }
}

/**
 * Загружает базу из localStorage. При отсутствии, повреждении или устаревшей
 * версии данных — возвращает корректное начальное состояние (seed).
 */
export function loadDatabase(): MockDatabase {
  if (!storageAvailable()) {
    return createSeedDatabase();
  }
  try {
    const raw = localStorage.getItem(MOCK_DB_STORAGE_KEY);
    if (!raw) {
      const seeded = createSeedDatabase();
      saveDatabase(seeded);
      return seeded;
    }
    const parsed: unknown = JSON.parse(raw);
    if (!isValidDatabase(parsed)) {
      console.warn('[mock-db] Повреждённые данные — выполняется восстановление из seed.');
      const seeded = createSeedDatabase();
      saveDatabase(seeded);
      return seeded;
    }
    return parsed;
  } catch (error) {
    console.warn('[mock-db] Не удалось прочитать данные — восстановление из seed.', error);
    const seeded = createSeedDatabase();
    saveDatabase(seeded);
    return seeded;
  }
}

/** Сохраняет базу в localStorage. */
export function saveDatabase(db: MockDatabase): void {
  if (!storageAvailable()) {
    return;
  }
  try {
    localStorage.setItem(MOCK_DB_STORAGE_KEY, JSON.stringify(db));
  } catch (error) {
    console.warn('[mock-db] Не удалось сохранить данные.', error);
  }
}

/** Полностью сбрасывает базу к начальному состоянию. */
export function resetDatabase(): MockDatabase {
  const seeded = createSeedDatabase();
  saveDatabase(seeded);
  return seeded;
}
