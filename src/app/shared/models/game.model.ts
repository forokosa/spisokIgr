/** Статус игры в библиотеке пользователя. */
export type GameStatus = 'in-progress' | 'completed' | 'want-to-play' | 'dropped';

/** Доменная модель игры. */
export interface Game {
  id: string;
  title: string;
  genre: string;
  platform: string;
  status: GameStatus;
  coverUrl?: string;
  description?: string;
  rating?: number;
  releaseYear?: number;
  addedAt: string;
  updatedAt: string;
}

/** Данные, необходимые для создания или редактирования игры. */
export interface GameDraft {
  title: string;
  genre: string;
  platform: string;
  status: GameStatus;
  coverUrl?: string;
  description?: string;
  rating?: number;
  releaseYear?: number;
}

/** Поля игры, доступные для частичного обновления. */
export type GamePatch = Partial<Omit<Game, 'id' | 'addedAt' | 'updatedAt'>>;

/** Параметры поиска и фильтрации библиотеки. */
export interface GameQuery {
  search?: string;
  status?: GameStatus | 'all';
}
