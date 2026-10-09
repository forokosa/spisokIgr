import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { Game, GameStatus } from '../../../../shared/models/game.model';
import { filterGames } from '../../../../shared/utils/game-filter';
import { GameCardComponent } from '../../../../shared/components/game-card/game-card';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { isMockApiError } from '../../../../mock/mock-error';

/** Список желаемого — игры в статусе «Хочу пройти». */
@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [RouterLink, GameCardComponent, EmptyStateComponent, ConfirmDialogComponent],
  templateUrl: './wishlist-page.html',
  styleUrl: './wishlist-page.scss',
})
export class WishlistPageComponent {
  private readonly gamesService = inject(GamesService);
  private readonly notifications = inject(NotificationService);

  protected readonly wishlist = this.gamesService.wishlist;
  protected readonly loading = this.gamesService.loading;
  protected readonly search = signal('');
  protected readonly busyId = signal<string | null>(null);
  protected readonly pendingRemoval = signal<Game | null>(null);

  protected readonly filtered = computed(() =>
    filterGames(this.wishlist(), { search: this.search() }),
  );

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected clearSearch(): void {
    this.search.set('');
  }

  protected async moveTo(game: Game, status: GameStatus): Promise<void> {
    this.busyId.set(game.id);
    try {
      await this.gamesService.changeStatus(game.id, status);
      const label = status === 'completed' ? 'пройденной' : 'игрой в процессе';
      this.notifications.success(`«${game.title}» отмечена ${label}`);
    } catch (error) {
      this.notifications.error(isMockApiError(error) ? error.message : 'Не удалось изменить статус');
    } finally {
      this.busyId.set(null);
    }
  }

  protected requestRemoval(game: Game): void {
    this.pendingRemoval.set(game);
  }

  protected cancelRemoval(): void {
    this.pendingRemoval.set(null);
  }

  protected async confirmRemoval(): Promise<void> {
    const game = this.pendingRemoval();
    if (!game) {
      return;
    }
    this.busyId.set(game.id);
    try {
      await this.gamesService.remove(game.id);
      this.notifications.success('Игра удалена из списка желаемого');
      this.pendingRemoval.set(null);
    } catch (error) {
      this.notifications.error(isMockApiError(error) ? error.message : 'Не удалось удалить игру');
    } finally {
      this.busyId.set(null);
    }
  }
}
