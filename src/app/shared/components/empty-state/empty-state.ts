import { Component, input } from '@angular/core';

/** Универсальное пустое состояние списка. */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `<div class="empty">
    <i class="{{ icon() }}" aria-hidden="true"></i>
    <h3 class="empty__title">{{ title() }}</h3>
    @if (message()) {
      <p class="empty__message">{{ message() }}</p>
    }
    <ng-content />
  </div>`,
  styles: [
    `
      .empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        text-align: center;
        gap: 10px;
        padding: 56px 24px;
        border: 1px dashed var(--gt-border);
        border-radius: 16px;
        background: rgba(11, 41, 68, 0.35);
      }

      .empty i {
        font-size: 44px;
        color: var(--gt-accent);
        opacity: 0.85;
      }

      .empty__title {
        font-size: 17px;
        font-weight: 700;
      }

      .empty__message {
        color: var(--gt-text-muted);
        max-width: 420px;
      }
    `,
  ],
})
export class EmptyStateComponent {
  readonly icon = input('pi pi-inbox');
  readonly title = input('Пусто');
  readonly message = input('');
}
