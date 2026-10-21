import { Routes } from '@angular/router';

export const GAMES_ROUTES: Routes = [
  {
    path: '',
    title: 'Мои игры — GameTrack',
    loadComponent: () =>
      import('./pages/games-list-page/games-list-page').then((m) => m.GamesListPageComponent),
  },
  {
    path: 'new',
    title: 'Новая игра — GameTrack',
    data: { mode: 'create' },
    loadComponent: () =>
      import('./pages/game-form-page/game-form-page').then((m) => m.GameFormPageComponent),
  },
  {
    path: ':id/edit',
    title: 'Редактирование игры — GameTrack',
    data: { mode: 'edit' },
    loadComponent: () =>
      import('./pages/game-form-page/game-form-page').then((m) => m.GameFormPageComponent),
  },
  {
    path: ':id',
    title: 'Игра — GameTrack',
    loadComponent: () =>
      import('./pages/game-detail-page/game-detail-page').then((m) => m.GameDetailPageComponent),
  },
];
