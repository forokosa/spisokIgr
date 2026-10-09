import { Component, computed, input } from '@angular/core';
import { GameStatus } from '../../models/game.model';
import { GAME_STATUS_META } from '../../constants/game-status';

/** Цветная пилюля статуса игры с иконкой. */
@Component({
  selector: 'app-status-pill',
  standalone: true,
  template: `<span
    class="gt-status"
    [style.color]="meta().color"
    [style.background]="background()"
    [style.borderColor]="border()"
  >
    <i class="{{ meta().icon }}" aria-hidden="true"></i>{{ meta().label }}
  </span>`,
})
export class StatusPillComponent {
  readonly status = input.required<GameStatus>();
  protected readonly meta = computed(() => GAME_STATUS_META[this.status()]);
  protected readonly background = computed(() => `${this.meta().color}22`);
  protected readonly border = computed(() => `${this.meta().color}55`);
}
