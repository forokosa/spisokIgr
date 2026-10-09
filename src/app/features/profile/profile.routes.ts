import { Routes } from '@angular/router';

export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    title: 'Профиль — GameTrack',
    loadComponent: () =>
      import('./pages/profile-page/profile-page').then((m) => m.ProfilePageComponent),
  },
];
