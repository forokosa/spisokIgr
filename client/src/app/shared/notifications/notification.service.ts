import { Injectable, signal } from '@angular/core';
import { AppNotification, NotificationKind } from './notification.model';

const DEFAULT_DURATION_MS = 4000;

/** Единый сервис всплывающих уведомлений об успехе и ошибках. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly items = signal<AppNotification[]>([]);
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();

  readonly notifications = this.items.asReadonly();

  success(message: string, duration = DEFAULT_DURATION_MS): void {
    this.push('success', message, duration);
  }

  error(message: string, duration = DEFAULT_DURATION_MS): void {
    this.push('error', message, duration);
  }

  info(message: string, duration = DEFAULT_DURATION_MS): void {
    this.push('info', message, duration);
  }

  dismiss(id: string): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
    this.items.update((list) => list.filter((item) => item.id !== id));
  }

  private push(kind: NotificationKind, message: string, duration: number): void {
    const id = `ntf_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`;
    this.items.update((list) => [...list, { id, kind, message }]);
    const timer = setTimeout(() => this.dismiss(id), duration);
    this.timers.set(id, timer);
  }
}
