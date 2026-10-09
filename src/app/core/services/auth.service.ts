import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthSession, LoginCredentials } from '../../shared/models';
import { loginHandler, RecoveryResult, requestRecoveryHandler } from '../../mock/auth/handlers';
import { clearSession, readSession, writeSession } from '../../mock/auth/session-store';
import { MockApiClient } from '../../mock/mock-api-client';

/** Управляет демонстрационной сессией пользователя. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly client = inject(MockApiClient);
  private readonly sessionSignal = signal<AuthSession | null>(readSession());

  readonly session = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.sessionSignal() !== null);

  async login(credentials: LoginCredentials): Promise<AuthSession> {
    const session = await this.client.request((db) => loginHandler(db, credentials));
    writeSession(session);
    this.sessionSignal.set(session);
    return session;
  }

  logout(): void {
    clearSession();
    this.sessionSignal.set(null);
  }

  requestRecovery(email: string): Promise<RecoveryResult> {
    return this.client.request((db) => requestRecoveryHandler(db, email));
  }
}
