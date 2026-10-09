import { GameStatus } from '../models/game.model';

/** Метаданные статуса: русская подпись, иконка и цвет акцента. */
export interface GameStatusMeta {
  value: GameStatus;
  label: string;
  shortLabel: string;
  icon: string;
  color: string;
}

export const GAME_STATUSES: readonly GameStatus[] = [
  'in-progress',
  'completed',
  'want-to-play',
  'dropped',
] as const;

/** Единый источник русских названий и цветов статусов для всего приложения. */
export const GAME_STATUS_META: Record<GameStatus, GameStatusMeta> = {
  'in-progress': {
    value: 'in-progress',
    label: 'В процессе',
    shortLabel: 'В процессе',
    icon: 'pi pi-play',
    color: '#4C8DFF',
  },
  completed: {
    value: 'completed',
    label: 'Пройдено',
    shortLabel: 'Пройдено',
    icon: 'pi pi-check-circle',
    color: '#3DD68C',
  },
  'want-to-play': {
    value: 'want-to-play',
    label: 'Хочу пройти',
    shortLabel: 'Хочу пройти',
    icon: 'pi pi-star',
    color: '#F5B544',
  },
  dropped: {
    value: 'dropped',
    label: 'Брошено',
    shortLabel: 'Брошено',
    icon: 'pi pi-times-circle',
    color: '#F0617A',
  },
};

export function gameStatusLabel(status: GameStatus): string {
  return GAME_STATUS_META[status].label;
}

export function gameStatusColor(status: GameStatus): string {
  return GAME_STATUS_META[status].color;
}

export interface GameStatusFilter {
  value: GameStatus | 'all';
  label: string;
}

/** Фильтры для страницы «Мои игры» и списка желаемого. */
export const GAME_STATUS_FILTERS: readonly GameStatusFilter[] = [
  { value: 'all', label: 'Все' },
  { value: 'in-progress', label: 'В процессе' },
  { value: 'completed', label: 'Пройдено' },
  { value: 'want-to-play', label: 'Хочу пройти' },
  { value: 'dropped', label: 'Брошено' },
] as const;
