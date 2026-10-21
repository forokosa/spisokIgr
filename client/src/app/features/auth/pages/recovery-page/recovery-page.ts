import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../shared/notifications/notification.service';
import { errorTextFor } from '../../../../shared/validators/error-texts';
import { isApiError } from '../../../../core/http/api-error';

/** Демонстрационный сценарий восстановления пароля. */
@Component({
  selector: 'app-recovery-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './recovery-page.html',
  styleUrl: './recovery-page.scss',
})
export class RecoveryPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  protected readonly submitting = signal(false);
  protected readonly sentTo = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
  });

  protected get emailControl() {
    return this.form.controls.email;
  }

  protected emailError(): string | null {
    return this.emailControl.touched ? errorTextFor(this.emailControl.errors) : null;
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    try {
      const { email } = this.form.getRawValue();
      const result = await this.auth.requestRecovery(email);
      this.sentTo.set(result.email);
      this.notifications.info('Демонстрационный запрос обработан');
    } catch (error) {
      const message = isApiError(error) ? error.message : 'Не удалось отправить запрос';
      this.notifications.error(message);
    } finally {
      this.submitting.set(false);
    }
  }
}
