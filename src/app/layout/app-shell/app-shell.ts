import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { GamesService } from '../../core/services/games.service';
import { AuthService } from '../../core/services/auth.service';
import { ProfileService } from '../../core/services/profile.service';
import { CoverImageComponent } from '../../shared/components/cover-image/cover-image';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

/** Оболочка авторизованной части приложения: боковая навигация, верхняя панель, контент. */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CoverImageComponent],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly profileService = inject(ProfileService);
  private readonly gamesService = inject(GamesService);
  private readonly router = inject(Router);

  readonly profile = this.profileService.profile;
  readonly searchTerm = computed(() => this.gamesService.query().search ?? '');

  readonly navOpen = signal(false);
  readonly userMenuOpen = signal(false);

  readonly navItems: readonly NavItem[] = [
    { label: 'Главная', path: '/dashboard', icon: 'pi pi-home' },
    { label: 'Мои игры', path: '/games', icon: 'pi pi-th-large' },
    { label: 'Список желаемого', path: '/wishlist', icon: 'pi pi-star' },
    { label: 'Статистика', path: '/statistics', icon: 'pi pi-chart-bar' },
    { label: 'Профиль', path: '/profile', icon: 'pi pi-user' },
  ];

  ngOnInit(): void {
    void this.profileService.load();
    void this.gamesService.load();
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.gamesService.setSearch(value);
    if (!this.router.url.startsWith('/games')) {
      void this.router.navigate(['/games']);
    }
  }

  protected toggleNav(): void {
    this.navOpen.update((open) => !open);
  }

  protected closeNav(): void {
    this.navOpen.set(false);
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.auth.logout();
    void this.router.navigate(['/auth/login']);
  }
}
