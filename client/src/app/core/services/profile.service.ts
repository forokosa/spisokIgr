import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, toApiError } from '../http/api-error';
import { ProfileDraft, UserProfile } from '../../shared/models';

/** Единый источник данных профиля пользователя (REST API). */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/profile`;

  private readonly profileSignal = signal<UserProfile | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);

  readonly profile = this.profileSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  /** Сбрасывает профиль при смене аккаунта. */
  reset(): void {
    this.profileSignal.set(null);
    this.loadingSignal.set(false);
    this.loadedSignal.set(false);
  }

  async load(force = false): Promise<void> {
    if (this.loadedSignal() && !force) {
      return;
    }

    this.loadingSignal.set(true);
    try {
      const profile = await firstValueFrom(
        this.http.get<UserProfile>(this.baseUrl).pipe(
          tap((response) => {
            if (!response || typeof response.id !== 'string') {
              throw new ApiError(500, 'Сервер вернул некорректный профиль.');
            }
            this.profileSignal.set(response);
            this.loadedSignal.set(true);
          }),
          catchError((error: unknown) => throwError(() => toApiError(error))),
        ),
      );
      void profile;
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async update(draft: ProfileDraft): Promise<UserProfile> {
    this.assertDraft(draft);

    const profile = await firstValueFrom(
      this.http.put<UserProfile>(this.baseUrl, draft).pipe(
        tap((response) => {
          if (!response || typeof response.id !== 'string') {
            throw new ApiError(500, 'Сервер вернул некорректный профиль.');
          }
          this.profileSignal.set(response);
          this.loadedSignal.set(true);
        }),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    return profile;
  }

  private assertDraft(draft: ProfileDraft): void {
    if (!draft) {
      throw new ApiError(400, 'Данные профиля обязательны.');
    }
    if (!draft.name || draft.name.trim().length < 2) {
      throw new ApiError(400, 'Имя должно содержать минимум 2 символа.');
    }
    if (!draft.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(draft.email.trim())) {
      throw new ApiError(400, 'Введите корректный email.');
    }
  }
}
