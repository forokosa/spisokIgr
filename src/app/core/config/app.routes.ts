import { Routes } from '@angular/router';
import { AppShellComponent } from '../../layout/app-shell/app-shell';
import { GuestShellComponent } from '../../layout/guest-shell/guest-shell';
import { NotFoundPageComponent } from '../../layout/not-found/not-found-page';
import { authGuard, guestGuard } from '../navigation/guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'auth',
    component: GuestShellComponent,
    canActivate: [guestGuard],
    loadChildren: () => import('../../features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('../../features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
      {
        path: 'games',
        loadChildren: () => import('../../features/games/games.routes').then((m) => m.GAMES_ROUTES),
      },
      {
        path: 'wishlist',
        loadChildren: () =>
          import('../../features/wishlist/wishlist.routes').then((m) => m.WISHLIST_ROUTES),
      },
      {
        path: 'statistics',
        loadChildren: () =>
          import('../../features/statistics/statistics.routes').then((m) => m.STATISTICS_ROUTES),
      },
      {
        path: 'profile',
        loadChildren: () =>
          import('../../features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
      },
      { path: '**', component: NotFoundPageComponent },
    ],
  },
  { path: '**', component: NotFoundPageComponent },
];
