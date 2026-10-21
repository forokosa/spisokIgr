import { Component, computed, inject, OnInit } from '@angular/core';
import { StatisticsService } from '../../../../core/services/statistics.service';
import { GAME_STATUS_META } from '../../../../shared/constants/game-status';
import { sharePercent } from '../../../../shared/utils/statistics';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

interface StatCard {
  label: string;
  value: number;
  icon: string;
  color: string;
}

/** Страница статистики коллекции. */
@Component({
  selector: 'app-statistics-page',
  standalone: true,
  imports: [EmptyStateComponent],
  templateUrl: './statistics-page.html',
  styleUrl: './statistics-page.scss',
})
export class StatisticsPageComponent implements OnInit {
  private readonly statisticsService = inject(StatisticsService);

  protected readonly stats = this.statisticsService.statistics;
  protected readonly statusMeta = GAME_STATUS_META;

  protected readonly cards = computed<StatCard[]>(() => {
    const stats = this.stats();
    return [
      { label: 'Всего игр', value: stats.total, icon: 'pi pi-th-large', color: '#4c45ff' },
      { label: 'Пройдено', value: stats.completed, icon: 'pi pi-check-circle', color: '#3dd68c' },
      { label: 'В процессе', value: stats.inProgress, icon: 'pi pi-play', color: '#4c8dff' },
      { label: 'Хочу пройти', value: stats.wantToPlay, icon: 'pi pi-star', color: '#f5b544' },
      { label: 'Брошено', value: stats.dropped, icon: 'pi pi-times-circle', color: '#f0617a' },
    ];
  });

  protected readonly maxGenre = computed(() =>
    Math.max(1, ...this.stats().byGenre.map((item) => item.count)),
  );

  protected readonly maxPlatform = computed(() =>
    Math.max(1, ...this.stats().byPlatform.map((item) => item.count)),
  );

  ngOnInit(): void {
    void this.statisticsService.load().catch(() => undefined);
  }

  protected percent(count: number): number {
    return sharePercent(count, this.stats().total);
  }

  protected width(count: number, max: number): number {
    return Math.round((count / max) * 100);
  }
}
