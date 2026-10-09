import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    title: 'Вход — GameTrack',
    loadComponent: () => import('./pages/login-page/login-page').then((m) => m.LoginPageComponent),
  },
  {
    path: 'recovery',
    title: 'Восстановление пароля — GameTrack',
    loadComponent: () =>
      import('./pages/recovery-page/recovery-page').then((m) => m.RecoveryPageComponent),
  },
];
