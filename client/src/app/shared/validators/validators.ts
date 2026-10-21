import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Рейтинг должен быть в диапазоне 0..10, если задан. */
export function ratingRangeValidator(min = 0, max = 10): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = control.value;
    if (raw === null || raw === undefined || raw === '') {
      return null;
    }
    const value = Number(raw);
    if (Number.isNaN(value)) {
      return { ratingRange: { min, max } };
    }
    if (value < min || value > max) {
      return { ratingRange: { min, max } };
    }
    return null;
  };
}

/** Год выпуска должен быть в разумных пределах, если задан. */
export function releaseYearValidator(min = 1970, max = new Date().getFullYear() + 1): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = control.value;
    if (raw === null || raw === undefined || raw === '') {
      return null;
    }
    const value = Number(raw);
    if (!Number.isInteger(value) || value < min || value > max) {
      return { releaseYear: { min, max } };
    }
    return null;
  };
}

/** Проверяет, что выбранный файл является изображением допустимого размера. */
export function imageFileValidator(maxMegabytes = 5): ValidatorFn {
  const maxBytes = maxMegabytes * 1024 * 1024;
  return (control: AbstractControl): ValidationErrors | null => {
    const file: unknown = control.value;
    if (file === null || file === undefined) {
      return null;
    }
    if (!(file instanceof File)) {
      return { imageFile: { reason: 'type' } };
    }
    if (!file.type.startsWith('image/')) {
      return { imageFile: { reason: 'type' } };
    }
    if (file.size > maxBytes) {
      return { imageFile: { reason: 'size', maxMegabytes } };
    }
    return null;
  };
}

/** Проверяет совпадение пароля и его подтверждения на уровне группы формы. */
export function passwordsMatchValidator(
  passwordKey = 'password',
  confirmPasswordKey = 'confirmPassword',
): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey)?.value as unknown;
    const confirmPassword = group.get(confirmPasswordKey)?.value as unknown;
    return password === confirmPassword ? null : { mismatch: true };
  };
}

/** Пароль должен содержать хотя бы одну букву и одну цифру. */
export function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const password = String(value);
    if (!/\p{L}/u.test(password) || !/\d/.test(password)) {
      return { passwordStrength: true };
    }
    return null;
  };
}
