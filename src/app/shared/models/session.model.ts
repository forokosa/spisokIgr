/** Демонстрационная сессия пользователя (хранится в браузере). */
export interface AuthSession {
  userId: string;
  email: string;
  name: string;
  issuedAt: string;
  remember: boolean;
}

/** Учётные данные, передаваемые в mock-сервис авторизации. */
export interface LoginCredentials {
  email: string;
  password: string;
  remember: boolean;
}
