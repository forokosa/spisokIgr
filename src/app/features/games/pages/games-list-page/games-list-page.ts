import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { GameStatus } from '../../../../shared/models/game.model';
import { GAME_STATUS_FILTERS } from '../../../../shared/constants/game-status';
import { GameCardComponent } from '../../../../shared/components/game-card/game-card';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

/** Библиотека игр пользователя с поиском и фильтрами. */
@Component({
  selector: 'app-games-list-page',
  standalone: true,
  imports: [RouterLink, GameCardComponent, EmptyStateComponent],
  templateUrl: './games-list-page.html',
  styleUrl: './games-list-page.scss',
})
export class GamesListPageComponent {
  private readonly gamesService = inject(GamesService);

  protected readonly games = this.gamesService.games;
  protected readonly filteredGames = this.gamesService.filteredGames;
  protected readonly loading = this.gamesService.loading;
  protected readonly query = this.gamesService.query;
  protected readonly filters = GAME_STATUS_FILTERS;
  protected readonly searchTerm = computed(() => this.query().search ?? '');

  protected setStatus(status: GameStatus | 'all'): void {
    this.gamesService.setStatus(status);
  }

  protected onSearchInput(event: Event): void {
    this.gamesService.setSearch((event.target as HTMLInputElement).value);
  }

  protected clearSearch(): void {
    this.gamesService.setSearch('');
  }

  protected clearFilters(): void {
    this.gamesService.resetQuery();
  }

  protected reload(): void {
    void this.gamesService.load(true);
  }
}
