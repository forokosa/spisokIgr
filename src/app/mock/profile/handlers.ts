import { ProfileDraft, UserProfile } from '../../shared/models';
import { nowIso } from '../ids';
import { MockApiError } from '../mock-error';
import { MockDatabase } from '../mock-db.model';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Возвращает профиль пользователя. */
export function getProfileHandler(db: MockDatabase): UserProfile {
  return { ...db.profile };
}

/** Обновляет профиль пользователя. */
export function updateProfileHandler(db: MockDatabase, draft: ProfileDraft): UserProfile {
  const name = draft.name.trim();
  const email = draft.email.trim();
  if (name.length < 2) {
    throw new MockApiError('validation', 'Имя должно содержать минимум 2 символа');
  }
  if (!EMAIL_PATTERN.test(email)) {
    throw new MockApiError('validation', 'Введите корректный email');
  }
  db.profile = {
    ...db.profile,
    name,
    email,
    avatarUrl: draft.avatarUrl ?? db.profile.avatarUrl,
    updatedAt: nowIso(),
  };
  return { ...db.profile };
}
