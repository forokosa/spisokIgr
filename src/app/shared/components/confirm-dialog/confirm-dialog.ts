import { Component, input, output } from '@angular/core';

/** Модальное окно подтверждения опасного действия. */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `@if (visible()) {
    <div class="backdrop" (click)="onCancel()">
      <div
        class="dialog"
        role="alertdialog"
        aria-modal="true"
        [attr.aria-label]="title()"
        (click)="$event.stopPropagation()"
      >
        <h2 class="dialog__title">{{ title() }}</h2>
        <p class="dialog__message">{{ message() }}</p>
        <div class="dialog__actions">
          <button type="button" class="gt-btn gt-btn--ghost" (click)="onCancel()">
            {{ cancelLabel() }}
          </button>
          <button type="button" class="gt-btn gt-btn--danger" [disabled]="busy()" (click)="onConfirm()">
            @if (busy()) {
              <span class="gt-spinner" aria-hidden="true"></span>
            }
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    </div>
  }`,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        z-index: 200;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 20px;
        background: rgba(2, 10, 20, 0.7);
        backdrop-filter: blur(3px);
        animation: fade 0.15s ease;
      }

      .dialog {
        width: min(440px, 100%);
        background: linear-gradient(160deg, #0e3153, #0b2944);
        border: 1px solid var(--gt-border-soft);
        border-radius: 16px;
        padding: 24px;
        box-shadow: 0 24px 60px rgba(0, 0, 0, 0.55);
        animation: pop 0.18s ease;
      }

      .dialog__title {
        font-size: 18px;
        font-weight: 700;
        margin-bottom: 8px;
      }

      .dialog__message {
        color: var(--gt-text-muted);
        margin-bottom: 22px;
      }

      .dialog__actions {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        flex-wrap: wrap;
      }

      @keyframes fade {
        from {
          opacity: 0;
        }
      }

      @keyframes pop {
        from {
          transform: translateY(8px) scale(0.98);
          opacity: 0;
        }
      }
    `,
  ],
})
export class ConfirmDialogComponent {
  readonly visible = input(false);
  readonly title = input('Подтвердите действие');
  readonly message = input('');
  readonly confirmLabel = input('Подтвердить');
  readonly cancelLabel = input('Отмена');
  readonly busy = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  protected onConfirm(): void {
    this.confirmed.emit();
  }

  protected onCancel(): void {
    this.cancelled.emit();
  }
}
