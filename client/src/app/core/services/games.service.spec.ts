import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { Game } from '../../shared/models';
import { GamesService } from './games.service';

const API = `${environment.apiUrl}/games`;

const GAME: Game = {
  id: '11111111-1111-4111-8111-111111111111',
  title: 'The Witcher 3: Wild Hunt',
  genre: 'RPG',
  platform: 'PC',
  status: 'completed',
  coverUrl: 'images/covers/witcher-3.svg',
  description: 'Описание',
  rating: 10,
  releaseYear: 2015,
  addedAt: '2024-01-05T12:00:00.000Z',
  updatedAt: '2024-03-10T18:30:00.000Z',
};

function page(items: Game[], pageNumber = 1, totalCount = items.length, totalPages = 1) {
  return {
    items,
    page: pageNumber,
    pageSize: 6,
    totalCount,
    totalPages,
    hasPrevious: pageNumber > 1,
    hasNext: pageNumber < totalPages,
  };
}

describe('GamesService (REST API)', () => {
  let service: GamesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GamesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('загружает первую страницу асинхронно через GET', async () => {
    const loading = service.load();

    const request = httpMock.expectOne(
      (req) => req.url === API && req.params.get('page') === '1' && req.params.get('pageSize') === '6',
    );
    expect(request.request.method).toBe('GET');
    request.flush(page([GAME], 1, 7, 2));

    await loading;

    expect(service.games()).toEqual([GAME]);
    expect(service.total()).toBe(7);
    expect(service.totalPages()).toBe(2);
    expect(service.loaded()).toBeTrue();
    expect(service.loading()).toBeFalse();
  });

  it('передаёт поиск и статус в query-параметрах', async () => {
    service.setSearch('witcher');
    service.setStatus('completed');

    const loading = service.reloadFromFirstPage();
    const request = httpMock.expectOne(
      (req) =>
        req.url === API &&
        req.params.get('search') === 'witcher' &&
        req.params.get('status') === 'completed',
    );
    request.flush(page([GAME]));

    await loading;

    expect(service.query()).toEqual({ search: 'witcher', status: 'completed' });
  });

  it('загружает игру по id через GET', async () => {
    const loading = service.fetchById(GAME.id);
    const request = httpMock.expectOne(`${API}/${GAME.id}`);
    expect(request.request.method).toBe('GET');
    request.flush(GAME);

    const game = await loading;

    expect(game.id).toBe(GAME.id);
    expect(service.findById(GAME.id)?.title).toBe(GAME.title);
  });

  it('преобразует HTTP-ошибку в ApiError', async () => {
    const loading = service.load();
    const request = httpMock.expectOne((req) => req.url === API);
    request.flush({ message: 'Некорректная страница' }, { status: 400, statusText: 'Bad Request' });

    await expectAsync(loading).toBeRejectedWithError('Некорректная страница');
  });

  it('валидирует параметры пагинации до запроса', async () => {
    await expectAsync(service.loadPage(0)).toBeRejected();
    httpMock.expectNone(API);
  });

  it('удаляет игру через DELETE и перезагружает страницу', async () => {
    const loading = service.remove(GAME.id);

    const deleteRequest = httpMock.expectOne(`${API}/${GAME.id}`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null, { status: 204, statusText: 'No Content' });

    await new Promise<void>((resolve) => setTimeout(resolve));

    const reload = httpMock.expectOne(
      (req) => req.url === API && req.params.get('pageSize') === '6',
    );
    reload.flush(page([], 1, 0, 0));

    await loading;

    const wishlist = httpMock.expectOne(
      (req) => req.url === API && req.params.get('status') === 'want-to-play',
    );
    wishlist.flush(page([]));

    expect(service.findById(GAME.id)).toBeUndefined();
  });
});
