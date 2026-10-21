import { AuthSession } from '../../shared/models';

/** Ключ хранения сессии пользователя. */
export const SESSION_KEY = 'gametrack.session.v3';

function storageAvailable(): boolean {
  try {
    return typeof localStorage !== 'undefined';
  } catch {
    return false;
  }
}

/** Читает сохранённую сессию, если она корректна. */
export function readSession(): AuthSession | null {
  if (!storageAvailable()) {
    return null;
  }
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) {
      return null;
    }
    const parsed: unknown = JSON.parse(raw);
    if (
      typeof parsed === 'object' &&
      parsed !== null &&
      typeof (parsed as AuthSession).userId === 'string' &&
      typeof (parsed as AuthSession).email === 'string'
    ) {
      return parsed as AuthSession;
    }
    localStorage.removeItem(SESSION_KEY);
    return null;
  } catch {
    return null;
  }
}

/** Сохраняет сессию в браузере. */
export function writeSession(session: AuthSession): void {
  if (!storageAvailable()) {
    return;
  }
  try {
    localStorage.setItem(SESSION_KEY, JSON.stringify(session));
  } catch (error) {
    console.warn('[session-store] Не удалось сохранить сессию.', error);
  }
}

/** Удаляет сессию. */
export function clearSession(): void {
  if (!storageAvailable()) {
    return;
  }
  localStorage.removeItem(SESSION_KEY);
}
