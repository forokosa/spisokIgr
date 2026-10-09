import { AuthSession, LoginCredentials } from '../../shared/models';
import { createId, nowIso } from '../ids';
import { MockApiError } from '../mock-error';
import { MockDatabase } from '../mock-db.model';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Имитация входа: любой корректный email и непустой пароль. */
export function loginHandler(db: MockDatabase, credentials: LoginCredentials): AuthSession {
  const email = credentials.email.trim();
  if (!EMAIL_PATTERN.test(email)) {
    throw new MockApiError('validation', 'Введите корректный email');
  }
  if (credentials.password.trim().length === 0) {
    throw new MockApiError('validation', 'Введите пароль');
  }
  const session: AuthSession = {
    userId: db.profile.id,
    email,
    name: db.profile.name,
    issuedAt: nowIso(),
    remember: credentials.remember,
  };
  return session;
}

export interface RecoveryResult {
  email: string;
  delivered: boolean;
  code: string;
}

/** Имитация запроса восстановления пароля. */
export function requestRecoveryHandler(db: MockDatabase, email: string): RecoveryResult {
  const normalized = email.trim();
  if (!EMAIL_PATTERN.test(normalized)) {
    throw new MockApiError('validation', 'Введите корректный email');
  }
  const code = Math.floor(100000 + Math.random() * 900000).toString();
  // В демо-режиме письмо не отправляется — код выводится в консоль.
  console.info(`[mock-email] Код восстановления для ${normalized}: ${code}`);
  void createId('recovery');
  void db;
  return { email: normalized, delivered: true, code };
}
