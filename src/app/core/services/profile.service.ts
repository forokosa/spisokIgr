import { inject, Injectable, signal } from '@angular/core';
import { ProfileDraft, UserProfile } from '../../shared/models';
import { getProfileHandler, updateProfileHandler } from '../../mock/profile/handlers';
import { MockApiClient } from '../../mock/mock-api-client';

/** Единый источник данных профиля пользователя. */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly client = inject(MockApiClient);
  private readonly profileSignal = signal<UserProfile | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);

  readonly profile = this.profileSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  async load(force = false): Promise<void> {
    if (this.loadedSignal() && !force) {
      return;
    }
    this.loadingSignal.set(true);
    try {
      const profile = await this.client.request((db) => getProfileHandler(db));
      this.profileSignal.set(profile);
      this.loadedSignal.set(true);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async update(draft: ProfileDraft): Promise<UserProfile> {
    const profile = await this.client.request((db) => updateProfileHandler(db, draft));
    this.profileSignal.set(profile);
    return profile;
  }
}
