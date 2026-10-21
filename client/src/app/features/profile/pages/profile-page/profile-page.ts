import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../../../../core/services/profile.service';
import { StatisticsService } from '../../../../core/services/statistics.service';
import { ImageStoreService } from '../../../../core/services/image-store.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { errorTextFor } from '../../../../shared/validators/error-texts';
import { formatDate } from '../../../../shared/utils/dates';
import { CoverImageComponent } from '../../../../shared/components/cover-image/cover-image';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { isApiError } from '../../../../core/http/api-error';

/** Экран профиля пользователя с редактированием данных. */
@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [ReactiveFormsModule, CoverImageComponent, EmptyStateComponent],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly statisticsService = inject(StatisticsService);
  private readonly imageStore = inject(ImageStoreService);
  private readonly notifications = inject(NotificationService);

  protected readonly profile = this.profileService.profile;
  protected readonly stats = this.statisticsService.statistics;
  protected readonly topGenres = this.statisticsService.topGenres;
  protected readonly favoriteGenre = this.statisticsService.favoriteGenre;
  protected readonly favoritePlatform = this.statisticsService.favoritePlatform;

  protected readonly editing = signal(false);
  protected readonly busy = signal(false);
  protected readonly avatarPreview = signal<string | undefined>(undefined);

  private selectedAvatar: File | null = null;
  private objectUrl: string | null = null;

  protected readonly form = this.fb.nonNullable.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(2)]),
    email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
  });

  constructor() {
    effect(() => {
      const profile = this.profile();
      if (profile) {
        this.form.patchValue(
          { name: profile.name, email: profile.email },
          { emitEvent: false },
        );
      }
    });
  }

  ngOnInit(): void {
    void this.profileService.load().catch(() => undefined);
    void this.statisticsService.load().catch(() => undefined);
  }

  protected joinedAt(value: string | undefined): string {
    return value ? formatDate(value) : '—';
  }

  protected errorFor(name: 'name' | 'email'): string | null {
    const control = this.form.controls[name];
    return control.touched ? errorTextFor(control.errors) : null;
  }

  protected startEdit(): void {
    const profile = this.profile();
    this.avatarPreview.set(profile?.avatarUrl);
    this.editing.set(true);
  }

  protected cancelEdit(): void {
    const profile = this.profile();
    this.form.reset({ name: profile?.name ?? '', email: profile?.email ?? '' });
    this.selectedAvatar = null;
    this.revokeObjectUrl();
    this.avatarPreview.set(profile?.avatarUrl);
    this.editing.set(false);
  }

  protected onAvatarInput(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }
    if (!file.type.startsWith('image/')) {
      this.notifications.error('Аватар должен быть изображением');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.notifications.error('Размер аватара не должен превышать 5 МБ');
      return;
    }
    this.selectedAvatar = file;
    this.revokeObjectUrl();
    this.objectUrl = URL.createObjectURL(file);
    this.avatarPreview.set(this.objectUrl);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    try {
      const raw = this.form.getRawValue();
      const avatarUrl = this.selectedAvatar
        ? await this.imageStore.saveImage(this.selectedAvatar)
        : this.profile()?.avatarUrl;
      await this.profileService.update({ name: raw.name, email: raw.email, avatarUrl });
      this.selectedAvatar = null;
      this.revokeObjectUrl();
      this.editing.set(false);
      this.notifications.success('Профиль обновлён');
    } catch (error) {
      this.notifications.error(
        isApiError(error) ? error.message : 'Не удалось сохранить профиль',
      );
    } finally {
      this.busy.set(false);
    }
  }

  private revokeObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
