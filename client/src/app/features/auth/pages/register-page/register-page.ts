import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { GamesService } from '../../../../core/services/games.service';
import { ProfileService } from '../../../../core/services/profile.service';
import { StatisticsService } from '../../../../core/services/statistics.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { errorTextFor } from '../../../../shared/validators/error-texts';
import {
  passwordStrengthValidator,
  passwordsMatchValidator,
} from '../../../../shared/validators/validators';
import { isApiError } from '../../../../core/http/api-error';

/** Экран регистрации нового аккаунта. */
@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss',
})
export class RegisterPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly games = inject(GamesService);
  private readonly statistics = inject(StatisticsService);
  private readonly profile = inject(ProfileService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  protected readonly showPassword = signal(false);
  protected readonly showConfirmPassword = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      name: this.fb.nonNullable.control('', [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ]),
      email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
      password: this.fb.nonNullable.control('', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
        passwordStrengthValidator(),
      ]),
      confirmPassword: this.fb.nonNullable.control('', [Validators.required]),
      remember: this.fb.nonNullable.control(true),
    },
    { validators: passwordsMatchValidator() },
  );

  protected get nameControl() {
    return this.form.controls.name;
  }

  protected get emailControl() {
    return this.form.controls.email;
  }

  protected get passwordControl() {
    return this.form.controls.password;
  }

  protected get confirmPasswordControl() {
    return this.form.controls.confirmPassword;
  }

  protected nameError(): string | null {
    return this.nameControl.touched ? errorTextFor(this.nameControl.errors) : null;
  }

  protected emailError(): string | null {
    return this.emailControl.touched ? errorTextFor(this.emailControl.errors) : null;
  }

  protected passwordError(): string | null {
    return this.passwordControl.touched ? errorTextFor(this.passwordControl.errors) : null;
  }

  protected confirmPasswordError(): string | null {
    if (!this.confirmPasswordControl.touched) {
      return null;
    }
    if (this.form.hasError('mismatch')) {
      return 'Пароли не совпадают';
    }
    return errorTextFor(this.confirmPasswordControl.errors);
  }

  protected togglePassword(): void {
    this.showPassword.update((value) => !value);
  }

  protected toggleConfirmPassword(): void {
    this.showConfirmPassword.update((value) => !value);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const { name, email, password, remember } = this.form.getRawValue();
      const session = await this.auth.register({ name, email, password, remember });
      this.resetUserData();
      this.notifications.success(`Аккаунт создан. Добро пожаловать, ${session.name}!`);
      await this.router.navigateByUrl('/dashboard');
    } catch (error) {
      const message = isApiError(error) ? error.message : 'Не удалось создать аккаунт';
      this.notifications.error(message);
    } finally {
      this.submitting.set(false);
    }
  }

  /** Убирает данные предыдущего аккаунта из кэшей сервисов. */
  private resetUserData(): void {
    this.games.reset();
    this.statistics.reset();
    this.profile.reset();
  }
}
