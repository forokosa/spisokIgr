import { HttpInterceptorFn } from '@angular/common/http';
import { readSession } from '../auth/session-store';

/**
 * Передаёт идентификатор аккаунта в API, чтобы backend вернул
 * библиотеку игр именно этого пользователя.
 */
export const userContextInterceptor: HttpInterceptorFn = (request, next) => {
  const session = readSession();
  if (!session?.userId) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { 'X-User-Id': session.userId },
    }),
  );
};
