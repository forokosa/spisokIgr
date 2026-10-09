import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { GameStatus } from '../../../../shared/models/game.model';
import { GAME_STATUSES, GAME_STATUS_FILTERS } from '../../../../shared/constants/game-status';
import { CoverImageComponent } from '../../../../shared/components/cover-image/cover-image';
import { StatusPillComponent } from '../../../../shared/components/status-pill/status-pill';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { formatDate } from '../../../../shared/utils/dates';
import { isMockApiError } from '../../../../mock/mock-error';

/** Страница подробной информации об игре. */
@Component({
  selector: 'app-game-detail-page',
  standalone: true,
  imports: [
    RouterLink,
    CoverImageComponent,
    StatusPillComponent,
    ConfirmDialogComponent,
    EmptyStateComponent,
  ],
  templateUrl: './game-detail-page.html',
  styleUrl: './game-detail-page.scss',
})
export class GameDetailPageComponent {
  private readonly gamesService = inject(GamesService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  protected readonly game = computed(() => this.gamesService.findById(this.id()));
  protected readonly statuses = GAME_STATUSES;
  protected readonly filterLabels = GAME_STATUS_FILTERS;

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly busy = signal(false);
  protected readonly confirmDelete = signal(false);
  protected readonly confirmWishlistRemove = signal(false);

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  protected statusLabel(status: GameStatus): string {
    return this.filterLabels.find((item) => item.value === status)?.label ?? status;
  }

  protected formatAdded(value: string): string {
    return formatDate(value);
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    if (untracked(() => this.gamesService.findById(id))) {
      this.loading.set(false);
      return;
    }
    try {
      await this.gamesService.fetchById(id);
    } catch (error) {
      this.notFound.set(true);
      if (isMockApiError(error) && error.code !== 'not-found') {
        this.notifications.error(error.message);
      }
    } finally {
      this.loading.set(false);
    }
  }

  protected async markCompleted(): Promise<void> {
    await this.changeStatus('completed', 'Игра отмечена как пройденная');
  }

  protected async onStatusChange(event: Event): Promise<void> {
    const status = (event.target as HTMLSelectElement).value as GameStatus;
    await this.changeStatus(status, 'Статус игры обновлён');
  }

  private async changeStatus(status: GameStatus, successMessage: string): Promise<void> {
    const game = this.game();
    if (!game || game.status === status) {
      return;
    }
    this.busy.set(true);
    try {
      await this.gamesService.changeStatus(game.id, status);
      this.notifications.success(successMessage);
    } catch (error) {
      this.notifications.error(isMockApiError(error) ? error.message : 'Не удалось изменить статус');
    } finally {
      this.busy.set(false);
    }
  }

  protected async toggleWishlist(): Promise<void> {
    const game = this.game();
    if (!game) {
      return;
    }
    if (game.status === 'want-to-play') {
      this.confirmWishlistRemove.set(true);
      return;
    }
    await this.changeStatus('want-to-play', 'Игра добавлена в список желаемого');
  }

  protected async onWishlistRemoveCancelled(): Promise<void> {
    this.confirmWishlistRemove.set(false);
  }

  protected async onWishlistRemoveConfirmed(): Promise<void> {
    const game = this.game();
    if (!game) {
      return;
    }
    this.busy.set(true);
    try {
      await this.gamesService.remove(game.id);
      this.confirmWishlistRemove.set(false);
      this.notifications.success('Игра удалена из списка желаемого');
      await this.router.navigate(['/wishlist']);
    } catch (error) {
      this.notifications.error(isMockApiError(error) ? error.message : 'Не удалось удалить игру');
    } finally {
      this.busy.set(false);
    }
  }

  protected requestDelete(): void {
    this.confirmDelete.set(true);
  }

  protected cancelDelete(): void {
    this.confirmDelete.set(false);
  }

  protected async onDeleteConfirmed(): Promise<void> {
    const game = this.game();
    if (!game) {
      return;
    }
    this.busy.set(true);
    try {
      await this.gamesService.remove(game.id);
      this.confirmDelete.set(false);
      this.notifications.success('Игра удалена из библиотеки');
      await this.router.navigate(['/games']);
    } catch (error) {
      this.notifications.error(isMockApiError(error) ? error.message : 'Не удалось удалить игру');
    } finally {
      this.busy.set(false);
    }
  }
}
