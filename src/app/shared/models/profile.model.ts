/** Профиль пользователя приложения. */
export interface UserProfile {
  id: string;
  name: string;
  email: string;
  avatarUrl?: string;
  createdAt: string;
  updatedAt: string;
}

/** Данные, доступные для редактирования в профиле. */
export interface ProfileDraft {
  name: string;
  email: string;
  avatarUrl?: string;
}
