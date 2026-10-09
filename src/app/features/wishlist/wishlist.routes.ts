import { Routes } from '@angular/router';

export const WISHLIST_ROUTES: Routes = [
  {
    path: '',
    title: 'Список желаемого — GameTrack',
    loadComponent: () =>
      import('./pages/wishlist-page/wishlist-page').then((m) => m.WishlistPageComponent),
  },
];
