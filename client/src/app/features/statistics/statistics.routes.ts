import { Routes } from '@angular/router';

export const STATISTICS_ROUTES: Routes = [
  {
    path: '',
    title: 'Статистика — GameTrack',
    loadComponent: () =>
      import('./pages/statistics-page/statistics-page').then((m) => m.StatisticsPageComponent),
  },
];
