/** Сессия пользователя (хранится в браузере). */
export interface AuthSession {
  userId: string;
  email: string;
  name: string;
  issuedAt: string;
  remember: boolean;
}

/** Учётные данные для входа. */
export interface LoginCredentials {
  email: string;
  password: string;
  remember: boolean;
}

/** Данные для создания нового аккаунта. */
export interface RegisterCredentials {
  name: string;
  email: string;
  password: string;
  remember: boolean;
}
