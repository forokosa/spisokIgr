/** Коды ошибок mock-слоя, имитирующие HTTP-статусы. */
export type MockErrorCode =
  | 'validation'
  | 'not-found'
  | 'conflict'
  | 'unauthorized'
  | 'storage';

/** Ошибка, которую выбрасывает mock-слой. */
export class MockApiError extends Error {
  readonly status: number;
  readonly code: MockErrorCode;

  constructor(code: MockErrorCode, message: string, status = 400) {
    super(message);
    this.name = 'MockApiError';
    this.status = status;
    this.code = code;
  }
}

export function isMockApiError(error: unknown): error is MockApiError {
  return error instanceof MockApiError;
}

/** Приводит любое исключение к MockApiError. */
export function normalizeMockError(error: unknown): MockApiError {
  if (isMockApiError(error)) {
    return error;
  }
  const message = error instanceof Error ? error.message : 'Неизвестная ошибка mock-слоя';
  return new MockApiError('storage', message, 500);
}
