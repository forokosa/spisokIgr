import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { StatisticsService } from '../../../../core/services/statistics.service';
import { ProfileService } from '../../../../core/services/profile.service';
import { Game } from '../../../../shared/models/game.model';
import { GameCardComponent } from '../../../../shared/components/game-card/game-card';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

interface SummaryCard {
  label: string;
  value: number;
  icon: string;
  accent: string;
}

/** Главная страница со сводкой и недавними играми. */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [RouterLink, GameCardComponent, EmptyStateComponent],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPageComponent {
  private readonly gamesService = inject(GamesService);
  private readonly statisticsService = inject(StatisticsService);
  private readonly profileService = inject(ProfileService);

  protected readonly games = this.gamesService.games;
  protected readonly loading = this.gamesService.loading;
  protected readonly profile = this.profileService.profile;
  protected readonly stats = this.statisticsService.statistics;

  protected readonly summary = computed<SummaryCard[]>(() => {
    const stats = this.stats();
    return [
      { label: 'Всего игр', value: stats.total, icon: 'pi pi-th-large', accent: '#4c45ff' },
      { label: 'В процессе', value: stats.inProgress, icon: 'pi pi-play', accent: '#4c8dff' },
      { label: 'Пройдено', value: stats.completed, icon: 'pi pi-check-circle', accent: '#3dd68c' },
      { label: 'Хочу пройти', value: stats.wantToPlay, icon: 'pi pi-star', accent: '#f5b544' },
    ];
  });

  /** Последние добавленные игры для блока «Недавние игры». */
  protected readonly recentGames = computed<Game[]>(() =>
    [...this.games()]
      .sort((a, b) => b.addedAt.localeCompare(a.addedAt))
      .slice(0, 5),
  );
}
