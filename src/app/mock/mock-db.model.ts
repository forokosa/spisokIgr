import { Game, UserProfile } from '../shared/models';

/** Структура локальной mock-БД, хранимой в браузере. */
export interface MockDatabase {
  version: number;
  games: Game[];
  profile: UserProfile;
}

/** Версия схемы данных mock-БД. */
export const MOCK_DB_VERSION = 1;

/** Ключ localStorage, под которым лежит база. */
export const MOCK_DB_STORAGE_KEY = 'gametrack.db.v1';
