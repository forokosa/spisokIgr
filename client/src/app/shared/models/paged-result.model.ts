import { Game } from '../../shared/models';

/** Ответ API со страницей данных. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export type GamePage = PagedResult<Game>;
