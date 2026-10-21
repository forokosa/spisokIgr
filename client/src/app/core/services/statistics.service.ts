import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, toApiError } from '../http/api-error';
import { LibraryStatistics } from '../../shared/models';

const EMPTY_STATISTICS: LibraryStatistics = {
  total: 0,
  completed: 0,
  inProgress: 0,
  wantToPlay: 0,
  dropped: 0,
  completionRate: 0,
  averageRating: null,
  byStatus: [],
  byGenre: [],
  byPlatform: [],
};

/** Статистика библиотеки, рассчитанная на backend в Domain-слое. */
@Injectable({ providedIn: 'root' })
export class StatisticsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/statistics`;

  private readonly statisticsSignal = signal<LibraryStatistics>(EMPTY_STATISTICS);
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);

  readonly statistics = this.statisticsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  readonly favoriteGenre = computed<string | null>(() => {
    const [first] = this.statistics().byGenre;
    return first ? first.genre : null;
  });

  readonly favoritePlatform = computed<string | null>(() => {
    const [first] = this.statistics().byPlatform;
    return first ? first.platform : null;
  });

  /** Дополнительные жанры пользователя (все, кроме самого частого). */
  readonly topGenres = computed(() => this.statistics().byGenre.slice(0, 5));

  /** Сбрасывает статистику при смене аккаунта. */
  reset(): void {
    this.statisticsSignal.set(EMPTY_STATISTICS);
    this.loadingSignal.set(false);
    this.loadedSignal.set(false);
  }

  async load(force = false): Promise<void> {
    if (this.loadedSignal() && !force) {
      return;
    }

    this.loadingSignal.set(true);
    try {
      const statistics = await firstValueFrom(
        this.http.get<LibraryStatistics>(this.baseUrl).pipe(
          tap((response) => {
            if (!response || typeof response.total !== 'number') {
              throw new ApiError(500, 'Сервер вернул некорректную статистику.');
            }
            this.statisticsSignal.set(response);
            this.loadedSignal.set(true);
          }),
          catchError((error: unknown) => throwError(() => toApiError(error))),
        ),
      );
      void statistics;
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
