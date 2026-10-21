import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, toApiError } from '../http/api-error';
import { clearSession, readSession, writeSession } from '../auth/session-store';
import { AuthSession, LoginCredentials, RegisterCredentials } from '../../shared/models';

export interface RecoveryResult {
  email: string;
  delivered: boolean;
  code: string;
}

/** Управляет сессией пользователя через REST API. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;
  private readonly sessionSignal = signal<AuthSession | null>(readSession());

  readonly session = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.sessionSignal() !== null);

  async login(credentials: LoginCredentials): Promise<AuthSession> {
    this.assertCredentials(credentials);

    const session = await firstValueFrom(
      this.http.post<AuthSession>(`${this.baseUrl}/login`, credentials).pipe(
        tap((response) => {
          if (!response || typeof response.userId !== 'string') {
            throw new ApiError(500, 'Сервер вернул некорректную сессию.');
          }
          writeSession(response);
          this.sessionSignal.set(response);
        }),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    return session;
  }

  /** Регистрирует аккаунт и сразу сохраняет полученную сессию. */
  async register(credentials: RegisterCredentials): Promise<AuthSession> {
    this.assertRegistration(credentials);

    const payload: RegisterCredentials = {
      name: credentials.name.trim(),
      email: credentials.email.trim(),
      password: credentials.password,
      remember: credentials.remember,
    };

    const session = await firstValueFrom(
      this.http.post<AuthSession>(`${this.baseUrl}/register`, payload).pipe(
        tap((response) => {
          if (!response || typeof response.userId !== 'string') {
            throw new ApiError(500, 'Сервер вернул некорректную сессию.');
          }
          writeSession(response);
          this.sessionSignal.set(response);
        }),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    return session;
  }

  logout(): void {
    clearSession();
    this.sessionSignal.set(null);
  }

  async requestRecovery(email: string): Promise<RecoveryResult> {
    const normalized = email?.trim() ?? '';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalized)) {
      throw new ApiError(400, 'Введите корректный email.');
    }

    return firstValueFrom(
      this.http.post<RecoveryResult>(`${this.baseUrl}/recovery`, { email: normalized }).pipe(
        tap((response) => {
          if (!response || typeof response.email !== 'string') {
            throw new ApiError(500, 'Сервер вернул некорректный ответ.');
          }
        }),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );
  }

  private assertCredentials(credentials: LoginCredentials): void {
    if (!credentials) {
      throw new ApiError(400, 'Учётные данные обязательны.');
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(credentials.email?.trim() ?? '')) {
      throw new ApiError(400, 'Введите корректный email.');
    }
    if (!credentials.password || credentials.password.length < 4) {
      throw new ApiError(400, 'Пароль должен содержать минимум 4 символа.');
    }
  }

  private assertRegistration(credentials: RegisterCredentials): void {
    if (!credentials) {
      throw new ApiError(400, 'Данные регистрации обязательны.');
    }

    const name = credentials.name?.trim() ?? '';
    if (name.length < 2 || name.length > 100) {
      throw new ApiError(400, 'Имя должно содержать от 2 до 100 символов.');
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(credentials.email?.trim() ?? '')) {
      throw new ApiError(400, 'Введите корректный email.');
    }

    const password = credentials.password ?? '';
    if (password.length < 8 || password.length > 128) {
      throw new ApiError(400, 'Пароль должен содержать от 8 до 128 символов.');
    }
    if (!/\p{L}/u.test(password) || !/\d/.test(password)) {
      throw new ApiError(400, 'Пароль должен содержать минимум одну букву и одну цифру.');
    }
  }
}
