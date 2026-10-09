import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Game } from '../../models/game.model';
import { StatusPillComponent } from '../status-pill/status-pill';
import { CoverImageComponent } from '../cover-image/cover-image';

/** Карточка игры для сеток библиотеки и списка желаемого. */
@Component({
  selector: 'app-game-card',
  standalone: true,
  imports: [RouterLink, StatusPillComponent, CoverImageComponent],
  template: `<a class="card" [routerLink]="['/games', game().id]">
    <div class="card__cover">
      <app-cover-image [src]="game().coverUrl" [alt]="'Обложка ' + game().title" />
      <span class="card__status"><app-status-pill [status]="game().status" /></span>
    </div>
    <div class="card__body">
      <h3 class="card__title" [title]="game().title">{{ game().title }}</h3>
      <p class="card__meta">{{ game().genre }} · {{ game().platform }}</p>
    </div>
  </a>`,
  styles: [
    `
      .card {
        display: flex;
        flex-direction: column;
        background: linear-gradient(165deg, #0b2944, #082033);
        border: 1px solid var(--gt-border);
        border-radius: 14px;
        overflow: hidden;
        height: 100%;
        transition:
          transform 0.18s ease,
          border-color 0.18s ease,
          box-shadow 0.18s ease;
      }

      .card:hover {
        transform: translateY(-4px);
        border-color: var(--gt-border-soft);
        box-shadow: 0 18px 36px rgba(0, 0, 0, 0.4);
      }

      .card__cover {
        position: relative;
        aspect-ratio: 3 / 4;
        background: #071f35;
      }

      .card__status {
        position: absolute;
        top: 10px;
        left: 10px;
        backdrop-filter: blur(4px);
      }

      .card__body {
        padding: 12px 14px 16px;
        display: flex;
        flex-direction: column;
        gap: 4px;
      }

      .card__title {
        font-size: 15px;
        font-weight: 700;
        line-height: 1.3;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }

      .card__meta {
        font-size: 12px;
        color: var(--gt-text-muted);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
    `,
  ],
})
export class GameCardComponent {
  readonly game = input.required<Game>();
}
