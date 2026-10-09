import { Injectable } from '@angular/core';
import { normalizeMockError } from './mock-error';
import { MockDatabase } from './mock-db.model';
import { loadDatabase, resetDatabase, saveDatabase } from './mock-db';

/** Обработчик mock-операции: получает текущую базу и возвращает результат. */
export type MockHandler<T> = (db: MockDatabase) => T;

export interface MockRequestOptions {
  /** Явная задержка в миллисекундах (по умолчанию случайная 300–600 мс). */
  delayMs?: number;
}

const MIN_DELAY_MS = 300;
const MAX_DELAY_MS = 600;

function randomDelay(): number {
  return MIN_DELAY_MS + Math.floor(Math.random() * (MAX_DELAY_MS - MIN_DELAY_MS + 1));
}

/**
 * Клиент mock-слоя. Имитирует асинхронные запросы к серверу: задержка,
 * изменение единой базы и сохранение в localStorage.
 */
@Injectable({ providedIn: 'root' })
export class MockApiClient {
  private db: MockDatabase = loadDatabase();

  /** Выполняет операцию над базой с имитацией сетевой задержки. */
  request<T>(handler: MockHandler<T>, options: MockRequestOptions = {}): Promise<T> {
    const delay = options.delayMs ?? randomDelay();
    return new Promise<T>((resolve, reject) => {
      setTimeout(() => {
        try {
          const result = handler(this.db);
          saveDatabase(this.db);
          resolve(result);
        } catch (error) {
          reject(normalizeMockError(error));
        }
      }, delay);
    });
  }

  /** Синхронное чтение из базы (используется для начальной загрузки). */
  select<T>(selector: (db: MockDatabase) => T): T {
    return selector(this.db);
  }

  /** Полный сброс базы к начальному состоянию. */
  reset(): void {
    this.db = resetDatabase();
  }
}
