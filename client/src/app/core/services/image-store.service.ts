import { Injectable } from '@angular/core';

const DB_NAME = 'gametrack-images';
const STORE_NAME = 'covers';
const DB_VERSION = 1;

function createImageId(): string {
  return typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

/**
 * Хранилище пользовательских изображений на основе IndexedDB.
 * Возвращает ссылку вида `idb:<id>`, которую затем можно разрешить в blob-URL.
 * Если IndexedDB недоступна, изображение хранится только в пределах сессии
 * (ссылка `mem:<id>`), о чём сообщает `isPersistent`.
 */
@Injectable({ providedIn: 'root' })
export class ImageStoreService {
  private dbPromise: Promise<IDBDatabase | null> | null = null;
  private readonly memory = new Map<string, Blob>();
  private readonly objectUrls = new Map<string, string>();

  /** Сохраняет изображение и возвращает ссылку на него. */
  async saveImage(file: Blob): Promise<string> {
    const db = await this.openDatabase();
    if (!db) {
      const id = createImageId();
      this.memory.set(id, file);
      return `mem:${id}`;
    }
    const id = createImageId();
    await new Promise<void>((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      tx.objectStore(STORE_NAME).put(file, id);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error ?? new Error('Не удалось сохранить изображение'));
    });
    return `idb:${id}`;
  }

  /** Разрешает ссылку на изображение в URL, пригодный для отображения. */
  async resolveUrl(reference: string | undefined): Promise<string | null> {
    if (!reference) {
      return null;
    }
    if (!reference.startsWith('idb:') && !reference.startsWith('mem:')) {
      return reference;
    }
    const [, id] = reference.split(':');
    if (!id) {
      return null;
    }
    const cached = this.objectUrls.get(id);
    if (cached) {
      return cached;
    }
    const blob = reference.startsWith('mem:')
      ? this.memory.get(id)
      : await this.readFromDatabase(id);
    if (!blob) {
      return null;
    }
    const url = URL.createObjectURL(blob);
    this.objectUrls.set(id, url);
    return url;
  }

  private async openDatabase(): Promise<IDBDatabase | null> {
    if (typeof indexedDB === 'undefined') {
      return null;
    }
    if (!this.dbPromise) {
      this.dbPromise = new Promise<IDBDatabase | null>((resolve) => {
        try {
          const request = indexedDB.open(DB_NAME, DB_VERSION);
          request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
              db.createObjectStore(STORE_NAME);
            }
          };
          request.onsuccess = () => resolve(request.result);
          request.onerror = () => resolve(null);
        } catch {
          resolve(null);
        }
      });
    }
    return this.dbPromise;
  }

  private async readFromDatabase(id: string): Promise<Blob | null> {
    const db = await this.openDatabase();
    if (!db) {
      return null;
    }
    return new Promise<Blob | null>((resolve) => {
      try {
        const tx = db.transaction(STORE_NAME, 'readonly');
        const request = tx.objectStore(STORE_NAME).get(id);
        request.onsuccess = () => resolve((request.result as Blob | undefined) ?? null);
        request.onerror = () => resolve(null);
      } catch {
        resolve(null);
      }
    });
  }
}
