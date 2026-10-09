import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Страница для неизвестных маршрутов. */
@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `<section class="not-found">
    <i class="pi pi-compass" aria-hidden="true"></i>
    <h1>Страница не найдена</h1>
    <p>Возможно, ссылка устарела или была введена с ошибкой.</p>
    <a class="gt-btn gt-btn--primary" routerLink="/dashboard">
      <i class="pi pi-home" aria-hidden="true"></i>На главную
    </a>
  </section>`,
  styles: [
    `
      .not-found {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 12px;
        text-align: center;
        min-height: 70vh;
        padding: 40px 20px;
      }

      .not-found i {
        font-size: 64px;
        color: var(--gt-accent);
        opacity: 0.85;
      }

      h1 {
        font-size: 24px;
        font-weight: 800;
      }

      p {
        color: var(--gt-text-muted);
        max-width: 380px;
      }

      a {
        margin-top: 12px;
      }
    `,
  ],
})
export class NotFoundPageComponent {}
