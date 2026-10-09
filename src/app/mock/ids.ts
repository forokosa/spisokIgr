/** Генерирует уникальный идентификатор с необязательным префиксом. */
export function createId(prefix = 'id'): string {
  const uuid =
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
  return `${prefix}_${uuid}`;
}

/** Текущая дата в формате ISO. */
export function nowIso(): string {
  return new Date().toISOString();
}
