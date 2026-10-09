import { computed, inject, Injectable, signal } from '@angular/core';
import { LibraryStatistics } from '../../shared/models';
import { computeStatistics } from '../../shared/utils/statistics';
import { GamesService } from './games.service';

/**
 * Производная статистика библиотеки. Пересчитывается автоматически
 * при любом изменении списка игр.
 */
@Injectable({ providedIn: 'root' })
export class StatisticsService {
  private readonly gamesService = inject(GamesService);

  readonly statistics = computed<LibraryStatistics>(() =>
    computeStatistics(this.gamesService.games()),
  );

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
}
