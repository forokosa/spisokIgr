import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Оболочка гостевых экранов (авторизация, восстановление пароля). */
@Component({
  selector: 'app-guest-shell',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
      }
    `,
  ],
})
export class GuestShellComponent {}
