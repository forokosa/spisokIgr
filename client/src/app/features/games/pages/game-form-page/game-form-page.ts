import { Component, computed, effect, inject, input, OnDestroy, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { GamesService } from '../../../../core/services/games.service';
import { ImageStoreService } from '../../../../core/services/image-store.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { GameStatus } from '../../../../shared/models/game.model';
import { GAME_GENRES } from '../../../../shared/constants/genres';
import { GAME_PLATFORMS } from '../../../../shared/constants/platforms';
import { GAME_STATUSES, GAME_STATUS_META } from '../../../../shared/constants/game-status';
import { errorTextFor } from '../../../../shared/validators/error-texts';
import {
  imageFileValidator,
  ratingRangeValidator,
  releaseYearValidator,
} from '../../../../shared/validators/validators';
import { CoverImageComponent } from '../../../../shared/components/cover-image/cover-image';
import { isApiError } from '../../../../core/http/api-error';

const MAX_COVER_MB = 5;

/** Форма добавления и редактирования игры. */
@Component({
  selector: 'app-game-form-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CoverImageComponent],
  templateUrl: './game-form-page.html',
  styleUrl: './game-form-page.scss',
})
export class GameFormPageComponent implements OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly gamesService = inject(GamesService);
  private readonly imageStore = inject(ImageStoreService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly mode = input<'create' | 'edit'>('create');
  readonly id = input<string | undefined>(undefined);

  protected readonly genres = GAME_GENRES;
  protected readonly platforms = GAME_PLATFORMS;
  protected readonly statuses = GAME_STATUSES;
  protected readonly statusMeta = GAME_STATUS_META;
  protected readonly maxCoverMb = MAX_COVER_MB;

  protected readonly busy = signal(false);
  protected readonly loading = signal(false);
  protected readonly dragActive = signal(false);
  protected readonly coverPreview = signal<string | undefined>(undefined);
  protected readonly isEdit = computed(() => this.mode() === 'edit');

  private selectedFile: File | null = null;
  private objectUrl: string | null = null;
  private loadedExisting = false;

  protected readonly form = this.fb.nonNullable.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(2)]),
    genre: this.fb.nonNullable.control('', [Validators.required]),
    platform: this.fb.nonNullable.control('', [Validators.required]),
    status: this.fb.nonNullable.control<GameStatus>('want-to-play', [Validators.required]),
    description: this.fb.nonNullable.control(''),
    rating: this.fb.control<number | null>(null, [ratingRangeValidator(0, 10)]),
    releaseYear: this.fb.control<number | null>(null, [releaseYearValidator()]),
  });

  constructor() {
    effect(() => {
      const mode = this.mode();
      const id = this.id();
      untracked(() => void this.initialize(mode, id));
    });
  }

  ngOnDestroy(): void {
    this.revokeObjectUrl();
  }

  protected errorFor(controlName: 'title' | 'genre' | 'platform' | 'rating' | 'releaseYear'): string | null {
    const control = this.form.controls[controlName];
    return control.touched ? errorTextFor(control.errors) : null;
  }

  protected onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.applyFile(file);
    }
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.applyFile(file);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(true);
  }

  protected onDragLeave(): void {
    this.dragActive.set(false);
  }

  protected clearCover(): void {
    this.selectedFile = null;
    this.revokeObjectUrl();
    this.coverPreview.set(undefined);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Проверьте правильность заполнения формы');
      return;
    }
    this.busy.set(true);
    try {
      const raw = this.form.getRawValue();
      let coverUrl = this.coverPreview();
      if (this.selectedFile) {
        coverUrl = await this.imageStore.saveImage(this.selectedFile);
      }
      const existing = this.isEdit() ? this.gamesService.findById(this.id() ?? '') : undefined;
      if (!this.selectedFile && !this.coverPreview()) {
        coverUrl = undefined;
      } else if (!this.selectedFile && existing) {
        coverUrl = existing.coverUrl;
      }

      const draft = {
        title: raw.title,
        genre: raw.genre,
        platform: raw.platform,
        status: raw.status,
        description: raw.description || undefined,
        rating: raw.rating ?? undefined,
        releaseYear: raw.releaseYear ?? undefined,
        coverUrl,
      };

      const game =
        this.isEdit() && this.id()
          ? await this.gamesService.update(this.id()!, draft)
          : await this.gamesService.create(draft);

      this.notifications.success(
        this.isEdit() ? 'Изменения сохранены' : 'Игра добавлена в библиотеку',
      );
      await this.router.navigate(['/games', game.id]);
    } catch (error) {
      this.notifications.error(
        isApiError(error) ? error.message : 'Не удалось сохранить игру',
      );
    } finally {
      this.busy.set(false);
    }
  }

  protected cancel(): void {
    if (this.isEdit() && this.id()) {
      void this.router.navigate(['/games', this.id()]);
      return;
    }
    void this.router.navigate(['/games']);
  }

  private applyFile(file: File): void {
    if (!file.type.startsWith('image/')) {
      this.notifications.error('Можно загрузить только изображение');
      return;
    }
    if (file.size > MAX_COVER_MB * 1024 * 1024) {
      this.notifications.error(`Размер изображения не должен превышать ${MAX_COVER_MB} МБ`);
      return;
    }
    this.selectedFile = file;
    this.revokeObjectUrl();
    this.objectUrl = URL.createObjectURL(file);
    this.coverPreview.set(this.objectUrl);
  }

  private revokeObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }

  private async initialize(mode: 'create' | 'edit', id: string | undefined): Promise<void> {
    if (mode !== 'edit' || !id || this.loadedExisting) {
      return;
    }
    this.loading.set(true);
    try {
      const game = this.gamesService.findById(id) ?? (await this.gamesService.fetchById(id));
      this.form.patchValue({
        title: game.title,
        genre: game.genre,
        platform: game.platform,
        status: game.status,
        description: game.description ?? '',
        rating: game.rating ?? null,
        releaseYear: game.releaseYear ?? null,
      });
      this.coverPreview.set(game.coverUrl);
      this.loadedExisting = true;
    } catch (error) {
      this.notifications.error(isApiError(error) ? error.message : 'Не удалось загрузить игру');
      await this.router.navigate(['/games']);
    } finally {
      this.loading.set(false);
    }
  }
}
