import { HttpClient, HttpParams } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, firstValueFrom, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, toApiError } from '../http/api-error';
import {
  Game,
  GameDraft,
  GamePage,
  GamePatch,
  GameQuery,
  GameStatus,
} from '../../shared/models';
import { GAME_STATUSES } from '../../shared/constants/game-status';

const MAX_PAGE_SIZE = 100;
const MAX_SEARCH_LENGTH = 100;
const DEFAULT_PAGE_SIZE = 6;

/**
 * Работа с библиотекой игр через REST API.
 * Все обращения к серверу асинхронные, каждое продолжается цепочкой `.pipe(...)`.
 */
@Injectable({ providedIn: 'root' })
export class GamesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/games`;

  private readonly gamesSignal = signal<Game[]>([]);
  private readonly wishlistSignal = signal<Game[]>([]);
  private readonly recentSignal = signal<Game[]>([]);
  private readonly cacheSignal = signal<Readonly<Record<string, Game>>>({});
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);
  private readonly querySignal = signal<GameQuery>({ search: '', status: 'all' });
  private readonly pageSignal = signal(1);
  private readonly pageSizeSignal = signal(DEFAULT_PAGE_SIZE);
  private readonly totalSignal = signal(0);
  private readonly totalPagesSignal = signal(0);

  readonly games = this.gamesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly loaded = this.loadedSignal.asReadonly();
  readonly query = this.querySignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly pageSize = this.pageSizeSignal.asReadonly();
  readonly total = this.totalSignal.asReadonly();
  readonly totalPages = this.totalPagesSignal.asReadonly();
  readonly hasPrevious = computed(() => this.pageSignal() > 1);
  readonly hasNext = computed(() => this.pageSignal() < this.totalPagesSignal());

  /** Сервер уже применил поиск и фильтр, поэтому список совпадает с результатом API. */
  readonly filteredGames = computed(() => this.gamesSignal());

  /** Игры в статусе «Хочу пройти» — отдельная пачка с сервера. */
  readonly wishlist = this.wishlistSignal.asReadonly();

  /** Последние обновлённые игры для главной страницы. */
  readonly recentGames = this.recentSignal.asReadonly();

  /** Сбрасывает локальное состояние при смене аккаунта. */
  reset(): void {
    this.gamesSignal.set([]);
    this.wishlistSignal.set([]);
    this.recentSignal.set([]);
    this.cacheSignal.set({});
    this.loadingSignal.set(false);
    this.loadedSignal.set(false);
    this.querySignal.set({ search: '', status: 'all' });
    this.pageSignal.set(1);
    this.pageSizeSignal.set(DEFAULT_PAGE_SIZE);
    this.totalSignal.set(0);
    this.totalPagesSignal.set(0);
  }

  /** Загружает текущую страницу библиотеки (один раз, если не указано иное). */
  async load(force = false): Promise<void> {
    if (this.loadedSignal() && !force) {
      return;
    }
    await this.loadPage(this.pageSignal());
  }

  /** Загружает конкретную страницу с текущими фильтрами. */
  async loadPage(page: number): Promise<void> {
    this.assertPage(page);
    const size = this.pageSizeSignal();
    this.assertPageSize(size);
    this.loadingSignal.set(true);

    try {
      const response = await this.requestPage(page, size, this.querySignal());
      if (response.items.length === 0 && response.page > 1 && response.totalPages > 0) {
        await this.loadPage(response.totalPages);
        return;
      }

      this.gamesSignal.set(response.items);
      this.pageSignal.set(response.page);
      this.pageSizeSignal.set(response.pageSize);
      this.totalSignal.set(response.totalCount);
      this.totalPagesSignal.set(response.totalPages);
      this.cacheItems(response.items);
      this.loadedSignal.set(true);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  /** Сбрасывает страницу на первую и перезагружает данные. */
  async reloadFromFirstPage(): Promise<void> {
    await this.loadPage(1);
  }

  /** Обновляет текущую страницу. */
  async reload(): Promise<void> {
    await this.loadPage(this.pageSignal());
  }

  async loadWishlist(): Promise<void> {
    const response = await this.requestPage(1, MAX_PAGE_SIZE, { status: 'want-to-play' });
    this.wishlistSignal.set(response.items);
    this.cacheItems(response.items);
  }

  async loadRecent(limit = 5): Promise<void> {
    if (!Number.isInteger(limit) || limit < 1 || limit > MAX_PAGE_SIZE) {
      throw new ApiError(400, `Количество последних игр должно быть от 1 до ${MAX_PAGE_SIZE}.`);
    }
    const response = await this.requestPage(1, limit, { status: 'all' });
    this.recentSignal.set(response.items);
    this.cacheItems(response.items);
  }

  /** Устанавливает поисковый запрос, не сбрасывая выбранный статус. */
  setSearch(search: string): void {
    if (search.length > MAX_SEARCH_LENGTH) {
      throw new ApiError(400, `Поисковый запрос не должен превышать ${MAX_SEARCH_LENGTH} символов.`);
    }
    this.querySignal.update((query) => ({ ...query, search }));
  }

  /** Устанавливает фильтр по статусу, не сбрасывая поиск. */
  setStatus(status: GameStatus | 'all'): void {
    if (status !== 'all' && !GAME_STATUSES.includes(status)) {
      throw new ApiError(400, 'Некорректный статус игры.');
    }
    this.querySignal.update((query) => ({ ...query, status }));
  }

  resetQuery(): void {
    this.querySignal.set({ search: '', status: 'all' });
  }

  /** Возвращает игру из локального кэша по идентификатору. */
  findById(id: string): Game | undefined {
    if (!id) {
      return undefined;
    }
    return this.cacheSignal()[id] ?? this.gamesSignal().find((game) => game.id === id);
  }

  /** Загружает игру напрямую с сервера (для страницы деталей). */
  async fetchById(id: string): Promise<Game> {
    this.assertId(id);

    const game = await firstValueFrom(
      this.http.get<Game>(`${this.baseUrl}/${id}`).pipe(
        tap((response) => this.upsertLocal(response)),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    return game;
  }

  async create(draft: GameDraft): Promise<Game> {
    this.assertDraft(draft);

    const game = await firstValueFrom(
      this.http.post<Game>(this.baseUrl, draft).pipe(
        tap((response) => this.upsertLocal(response)),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    await this.reloadFromFirstPage();
    void this.loadWishlist().catch(() => undefined);
    return game;
  }

  async update(id: string, draft: GameDraft): Promise<Game> {
    this.assertId(id);
    this.assertDraft(draft);

    const game = await firstValueFrom(
      this.http.put<Game>(`${this.baseUrl}/${id}`, draft).pipe(
        tap((response) => this.upsertLocal(response)),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    await this.reload();
    void this.loadWishlist().catch(() => undefined);
    return game;
  }

  /** Частичное обновление: меняется только статус игры. */
  async changeStatus(id: string, status: GameStatus): Promise<Game> {
    this.assertId(id);
    if (!GAME_STATUSES.includes(status)) {
      throw new ApiError(400, 'Некорректный статус игры.');
    }

    const game = await firstValueFrom(
      this.http.patch<Game>(`${this.baseUrl}/${id}/status`, { status }).pipe(
        tap((response) => this.upsertLocal(response)),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    await this.reload();
    void this.loadWishlist().catch(() => undefined);
    void this.loadRecent().catch(() => undefined);
    return game;
  }

  async remove(id: string): Promise<void> {
    this.assertId(id);

    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
        tap(() => this.removeLocal(id)),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );

    await this.reload();
    void this.loadWishlist().catch(() => undefined);
  }

  private async requestPage(page: number, pageSize: number, query: GameQuery): Promise<GamePage> {
    this.assertPage(page);
    this.assertPageSize(pageSize);

    const search = (query.search ?? '').trim();
    if (search.length > MAX_SEARCH_LENGTH) {
      throw new ApiError(400, `Поисковый запрос не должен превышать ${MAX_SEARCH_LENGTH} символов.`);
    }

    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.length > 0) {
      params = params.set('search', search);
    }
    if (query.status && query.status !== 'all') {
      params = params.set('status', query.status);
    }

    return firstValueFrom(
      this.http.get<GamePage>(this.baseUrl, { params }).pipe(
        tap((response) => {
          if (!response || !Array.isArray(response.items)) {
            throw new ApiError(500, 'Сервер вернул некорректный ответ.');
          }
        }),
        catchError((error: unknown) => throwError(() => toApiError(error))),
      ),
    );
  }

  private cacheItems(items: readonly Game[]): void {
    this.cacheSignal.update((cache) => {
      const next = { ...cache };
      for (const game of items) {
        next[game.id] = game;
      }
      return next;
    });
  }

  private upsertLocal(game: Game): void {
    if (!game || typeof game.id !== 'string' || game.id.length === 0) {
      throw new ApiError(500, 'Сервер вернул некорректную игру.');
    }

    this.cacheSignal.update((cache) => ({ ...cache, [game.id]: game }));
    this.gamesSignal.update((games) => {
      const index = games.findIndex((item) => item.id === game.id);
      if (index < 0) {
        return games;
      }
      const next = [...games];
      next[index] = game;
      return next;
    });
    this.wishlistSignal.update((games) =>
      games.some((item) => item.id === game.id)
        ? games.map((item) => (item.id === game.id ? game : item))
        : games,
    );
  }

  private removeLocal(id: string): void {
    this.cacheSignal.update((cache) => {
      const next = { ...cache };
      delete next[id];
      return next;
    });
    this.gamesSignal.update((games) => games.filter((game) => game.id !== id));
    this.wishlistSignal.update((games) => games.filter((game) => game.id !== id));
    this.recentSignal.update((games) => games.filter((game) => game.id !== id));
  }

  private assertId(id: string): void {
    if (!id || id.trim().length === 0) {
      throw new ApiError(400, 'Идентификатор игры обязателен.');
    }
  }

  private assertPage(page: number): void {
    if (!Number.isInteger(page) || page < 1) {
      throw new ApiError(400, 'Номер страницы должен быть целым числом не меньше 1.');
    }
  }

  private assertPageSize(pageSize: number): void {
    if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > MAX_PAGE_SIZE) {
      throw new ApiError(400, `Размер страницы должен быть от 1 до ${MAX_PAGE_SIZE}.`);
    }
  }

  private assertDraft(draft: GameDraft): void {
    if (!draft) {
      throw new ApiError(400, 'Данные игры обязательны.');
    }
    if (!draft.title || draft.title.trim().length < 2) {
      throw new ApiError(400, 'Название должно содержать минимум 2 символа.');
    }
    if (!draft.genre || draft.genre.trim().length === 0) {
      throw new ApiError(400, 'Выберите жанр.');
    }
    if (!draft.platform || draft.platform.trim().length === 0) {
      throw new ApiError(400, 'Выберите платформу.');
    }
    if (!GAME_STATUSES.includes(draft.status)) {
      throw new ApiError(400, 'Некорректный статус игры.');
    }
    if (draft.rating !== undefined && (draft.rating < 0 || draft.rating > 10)) {
      throw new ApiError(400, 'Рейтинг должен быть от 0 до 10.');
    }
    if (
      draft.releaseYear !== undefined &&
      (!Number.isInteger(draft.releaseYear) || draft.releaseYear < 1970 || draft.releaseYear > 2100)
    ) {
      throw new ApiError(400, 'Некорректный год выпуска.');
    }
  }
}
