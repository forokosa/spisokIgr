import { Component, inject } from '@angular/core';
import { NotificationKind } from '../../notifications/notification.model';
import { NotificationService } from '../../notifications/notification.service';

const ICONS: Record<NotificationKind, string> = {
  success: 'pi pi-check-circle',
  error: 'pi pi-exclamation-circle',
  info: 'pi pi-info-circle',
};

/** Контейнер всплывающих уведомлений (монтируется в корне приложения). */
@Component({
  selector: 'app-toast-host',
  standalone: true,
  template: `<div class="toasts" role="status" aria-live="polite">
    @for (item of notifications(); track item.id) {
      <div class="toast" [class]="'toast--' + item.kind">
        <i class="{{ icons[item.kind] }}" aria-hidden="true"></i>
        <span class="toast__text">{{ item.message }}</span>
        <button type="button" class="toast__close" aria-label="Закрыть уведомление" (click)="dismiss(item.id)">
          <i class="pi pi-times" aria-hidden="true"></i>
        </button>
      </div>
    }
  </div>`,
  styles: [
    `
      .toasts {
        position: fixed;
        top: 18px;
        right: 18px;
        z-index: 300;
        display: flex;
        flex-direction: column;
        gap: 10px;
        width: min(360px, calc(100vw - 36px));
        pointer-events: none;
      }

      .toast {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 12px 14px;
        border-radius: 12px;
        background: #0e3153;
        border: 1px solid var(--gt-border);
        box-shadow: 0 16px 36px rgba(0, 0, 0, 0.45);
        animation: slide 0.2s ease;
        pointer-events: auto;
      }

      .toast i {
        font-size: 18px;
      }

      .toast__text {
        flex: 1;
        font-size: 13.5px;
        font-weight: 600;
      }

      .toast__close {
        border: 0;
        background: transparent;
        color: var(--gt-text-muted);
        padding: 4px;
        border-radius: 6px;
      }

      .toast__close:hover {
        background: rgba(255, 255, 255, 0.08);
        color: var(--gt-text);
      }

      .toast--success {
        border-color: rgba(61, 214, 140, 0.5);
      }

      .toast--success i {
        color: var(--gt-success);
      }

      .toast--error {
        border-color: rgba(240, 97, 122, 0.5);
      }

      .toast--error i {
        color: var(--gt-danger);
      }

      .toast--info i {
        color: var(--gt-info);
      }

      @keyframes slide {
        from {
          transform: translateX(16px);
          opacity: 0;
        }
      }
    `,
  ],
})
export class ToastHostComponent {
  private readonly service = inject(NotificationService);
  protected readonly notifications = this.service.notifications;
  protected readonly icons = ICONS;

  protected dismiss(id: string): void {
    this.service.dismiss(id);
  }
}
