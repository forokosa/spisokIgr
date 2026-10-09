/** Русские тексты ошибок для контролов форм. */
export const ERROR_TEXTS: Record<string, string> = {
  required: 'Поле обязательно для заполнения',
  email: 'Введите корректный email',
  minlength: 'Значение слишком короткое',
  maxlength: 'Значение слишком длинное',
  ratingRange: 'Рейтинг должен быть от 0 до 10',
  releaseYear: 'Укажите корректный год выпуска',
  imageFile: 'Загрузите изображение размером до 5 МБ',
  mismatch: 'Пароли не совпадают',
};

/** Возвращает человекочитаемый текст ошибки по объекту ошибок Angular Forms. */
export function errorTextFor(errors: Record<string, unknown> | null | undefined): string | null {
  if (!errors) {
    return null;
  }
  for (const key of Object.keys(errors)) {
    if (key === 'imageFile') {
      const details = errors[key];
      if (details && typeof details === 'object' && 'reason' in details) {
        const reason = (details as { reason?: string }).reason;
        if (reason === 'size') {
          return ERROR_TEXTS['imageFile'] ?? null;
        }
        return 'Файл должен быть изображением';
      }
    }
    const text = ERROR_TEXTS[key];
    if (text) {
      return text;
    }
  }
  return null;
}
