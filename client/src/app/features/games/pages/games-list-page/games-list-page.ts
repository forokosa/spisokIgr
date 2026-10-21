import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { GameStatus } from '../../../../shared/models/game.model';
import { GAME_STATUS_FILTERS } from '../../../../shared/constants/game-status';
import { GameCardComponent } from '../../../../shared/components/game-card/game-card';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { isApiError } from '../../../../core/http/api-error';

/** Библиотека игр пользователя с поиском, фильтрами и пагинацией. */
@Component({
  selector: 'app-games-list-page',
  standalone: true,
  imports: [RouterLink, GameCardComponent, EmptyStateComponent],
  templateUrl: './games-list-page.html',
  styleUrl: './games-list-page.scss',
})
export class GamesListPageComponent {
  private readonly gamesService = inject(GamesService);
  private readonly notifications = inject(NotificationService);

  protected readonly games = this.gamesService.games;
  protected readonly filteredGames = this.gamesService.filteredGames;
  protected readonly loading = this.gamesService.loading;
  protected readonly query = this.gamesService.query;
  protected readonly page = this.gamesService.page;
  protected readonly total = this.gamesService.total;
  protected readonly totalPages = this.gamesService.totalPages;
  protected readonly hasPrevious = this.gamesService.hasPrevious;
  protected readonly hasNext = this.gamesService.hasNext;
  protected readonly filters = GAME_STATUS_FILTERS;
  protected readonly searchTerm = computed(() => this.query().search ?? '');
  protected readonly pages = computed(() =>
    Array.from({ length: Math.max(0, this.totalPages()) }, (_, index) => index + 1),
  );

  protected async setStatus(status: GameStatus | 'all'): Promise<void> {
    this.gamesService.setStatus(status);
    await this.reloadFromFirstPage();
  }

  protected async onSearchInput(event: Event): Promise<void> {
    this.gamesService.setSearch((event.target as HTMLInputElement).value);
    await this.reloadFromFirstPage();
  }

  protected async clearSearch(): Promise<void> {
    this.gamesService.setSearch('');
    await this.reloadFromFirstPage();
  }

  protected async clearFilters(): Promise<void> {
    this.gamesService.resetQuery();
    await this.reloadFromFirstPage();
  }

  protected async reload(): Promise<void> {
    await this.reloadFromFirstPage();
  }

  protected async goToPage(page: number): Promise<void> {
    if (page < 1 || page > Math.max(1, this.totalPages())) {
      return;
    }
    await this.loadPage(page);
  }

  protected async previousPage(): Promise<void> {
    await this.goToPage(this.page() - 1);
  }

  protected async nextPage(): Promise<void> {
    await this.goToPage(this.page() + 1);
  }

  private async reloadFromFirstPage(): Promise<void> {
    await this.loadPage(1);
  }

  private async loadPage(page: number): Promise<void> {
    try {
      await this.gamesService.loadPage(page);
    } catch (error) {
      this.notifications.error(
        isApiError(error) ? error.message : 'Не удалось загрузить список игр',
      );
    }
  }
}
