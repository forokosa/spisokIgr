import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { errorTextFor } from '../../../../shared/validators/error-texts';
import { isMockApiError } from '../../../../mock/mock-error';
import { SEED_CREDENTIALS } from '../../../../mock/seed';

/** Экран входа в приложение. */
@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);

  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: this.fb.nonNullable.control(SEED_CREDENTIALS.email, [
      Validators.required,
      Validators.email,
    ]),
    password: this.fb.nonNullable.control(SEED_CREDENTIALS.password, [
      Validators.required,
      Validators.minLength(4),
    ]),
    remember: this.fb.nonNullable.control(true),
  });

  protected get emailControl() {
    return this.form.controls.email;
  }

  protected get passwordControl() {
    return this.form.controls.password;
  }

  protected emailError(): string | null {
    return this.emailControl.touched ? errorTextFor(this.emailControl.errors) : null;
  }

  protected passwordError(): string | null {
    return this.passwordControl.touched ? errorTextFor(this.passwordControl.errors) : null;
  }

  protected togglePassword(): void {
    this.showPassword.update((value) => !value);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    try {
      const { email, password, remember } = this.form.getRawValue();
      await this.auth.login({ email, password, remember });
      this.notifications.success('Вы вошли в демонстрационный профиль');
      const redirect = this.route.snapshot.queryParamMap.get('redirect');
      const target = redirect && redirect.startsWith('/') ? redirect : '/dashboard';
      await this.router.navigateByUrl(target);
    } catch (error) {
      const message = isMockApiError(error) ? error.message : 'Не удалось выполнить вход';
      this.notifications.error(message);
    } finally {
      this.submitting.set(false);
    }
  }
}
