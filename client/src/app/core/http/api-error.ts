import { HttpErrorResponse } from '@angular/common/http';

/** Ошибка обращения к REST API. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly code?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

interface ApiErrorBody {
  message?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

/** Приводит ответ сервера/сетевое исключение к единому ApiError. */
export function toApiError(error: unknown): ApiError {
  if (isApiError(error)) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    const body = isApiErrorBody(error.error) ? error.error : undefined;
    const validationMessage = body?.errors
      ? Object.values(body.errors).flat().find((message) => message.length > 0)
      : undefined;
    const message =
      body?.message ??
      body?.detail ??
      validationMessage ??
      body?.title ??
      (error.status === 0
        ? 'Сервер недоступен. Проверьте, что backend запущен на http://localhost:5080.'
        : `Ошибка запроса (${error.status}).`);

    return new ApiError(error.status, message);
  }

  if (error instanceof Error) {
    return new ApiError(0, error.message);
  }

  return new ApiError(0, 'Неизвестная ошибка при обращении к API.');
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null;
}
