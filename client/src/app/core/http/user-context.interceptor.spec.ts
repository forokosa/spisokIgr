import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { clearSession, writeSession } from '../auth/session-store';
import { AuthSession } from '../../shared/models';
import { userContextInterceptor } from './user-context.interceptor';

const SESSION: AuthSession = {
  userId: '11111111-1111-4111-8111-111111111111',
  email: 'player@gametrack.app',
  name: 'Новый Игрок',
  issuedAt: '2026-10-21T12:00:00.000Z',
  remember: true,
};

describe('userContextInterceptor', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    clearSession();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([userContextInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    clearSession();
  });

  it('добавляет X-User-Id из сохранённой сессии', async () => {
    writeSession(SESSION);
    const http = TestBed.inject(HttpClient);

    const response = firstValueFrom(http.get('/api/games'));
    const request = httpMock.expectOne('/api/games');
    expect(request.request.headers.get('X-User-Id')).toBe(SESSION.userId);
    request.flush({});

    await response;
  });

  it('не добавляет заголовок без сессии', async () => {
    const http = TestBed.inject(HttpClient);

    const response = firstValueFrom(http.get('/api/games'));
    const request = httpMock.expectOne('/api/games');
    expect(request.request.headers.has('X-User-Id')).toBeFalse();
    request.flush({});

    await response;
  });
});
