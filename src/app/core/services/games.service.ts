import { computed, inject, Injectable, signal } from '@angular/core';
import { Game, GameDraft, GamePatch, GameQuery, GameStatus } from '../../shared/models';
import { filterGames } from '../../shared/utils/game-filter';
import {
  changeGameStatusHandler,
  createGameHandler,
  deleteGameHandler,
  getGameHandler,
  listGamesHandler,
  updateGameHandler,
} from '../../mock/games/handlers';
import { MockApiClient } from '../../mock/mock-api-client';

/**
 * Единый источник данных о библиотеке игр.
 * Компоненты читают сигналы, а изменения проходят через mock-слой,
 * что обеспечивает согласованность всех экранов без перезагрузки.
 */
@Injectable({ providedIn: 'root' })
export class GamesService {
  private readonly client = inject(MockApiClient);

  private readonly gamesSignal = signal<Game[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);
  private readonly querySignal = signal<GameQuery>({ search: '', status: 'all' });

  readonly games = this.gamesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly loaded = this.loadedSignal.asReadonly();
  readonly query = this.querySignal.asReadonly();

  /** Библиотека с учётом текущего поиска и фильтра (реактивно). */
  readonly filteredGames = computed(() => filterGames(this.gamesSignal(), this.querySignal()));

  /** Игры в статусе «Хочу пройти» — основа списка желаемого. */
  readonly wishlist = computed(() =>
    filterGames(this.gamesSignal(), { status: 'want-to-play' }),
  );

  /** Загружает библиотеку из mock-слоя (один раз, если не указано иное). */
  async load(force = false): Promise<void> {
    if (this.loadedSignal() && !force) {
      return;
    }
    this.loadingSignal.set(true);
    try {
      const games = await this.client.request((db) => listGamesHandler(db, {}));
      this.gamesSignal.set(games);
      this.loadedSignal.set(true);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  /** Устанавливает поисковый запрос, не сбрасывая выбранный статус. */
  setSearch(search: string): void {
    this.querySignal.update((query) => ({ ...query, search }));
  }

  /** Устанавливает фильтр по статусу, не сбрасывая поиск. */
  setStatus(status: GameStatus | 'all'): void {
    this.querySignal.update((query) => ({ ...query, status }));
  }

  resetQuery(): void {
    this.querySignal.set({ search: '', status: 'all' });
  }

  /** Возвращает игру из локального кэша по идентификатору. */
  findById(id: string): Game | undefined {
    return this.gamesSignal().find((game) => game.id === id);
  }

  /** Загружает игру напрямую из mock-слоя (для страницы деталей). */
  async fetchById(id: string): Promise<Game> {
    const game = await this.client.request((db) => getGameHandler(db, id));
    this.upsertLocal(game);
    return game;
  }

  async create(draft: GameDraft): Promise<Game> {
    const game = await this.client.request((db) => createGameHandler(db, draft));
    this.upsertLocal(game);
    return game;
  }

  async update(id: string, patch: GamePatch): Promise<Game> {
    const game = await this.client.request((db) => updateGameHandler(db, id, patch));
    this.upsertLocal(game);
    return game;
  }

  async changeStatus(id: string, status: GameStatus): Promise<Game> {
    const game = await this.client.request((db) => changeGameStatusHandler(db, id, status));
    this.upsertLocal(game);
    return game;
  }

  async remove(id: string): Promise<void> {
    await this.client.request((db) => deleteGameHandler(db, id));
    this.gamesSignal.update((games) => games.filter((game) => game.id !== id));
  }

  private upsertLocal(game: Game): void {
    this.gamesSignal.update((games) => {
      const exists = games.some((item) => item.id === game.id);
      if (exists) {
        return games.map((item) => (item.id === game.id ? game : item));
      }
      return [game, ...games];
    });
  }
}
