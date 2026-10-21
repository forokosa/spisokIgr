import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthSession } from '../../shared/models';
import { clearSession } from '../auth/session-store';
import { AuthService } from './auth.service';

const API = `${environment.apiUrl}/auth`;

const SESSION: AuthSession = {
  userId: '11111111-1111-4111-8111-111111111111',
  email: 'player@gametrack.app',
  name: 'Новый Игрок',
  issuedAt: '2026-10-21T12:00:00.000Z',
  remember: true,
};

describe('AuthService (REST API)', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    clearSession();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    clearSession();
  });

  it('регистрирует аккаунт через POST и сохраняет сессию', async () => {
    const loading = service.register({
      name: '  Новый Игрок  ',
      email: '  player@gametrack.app ',
      password: 'password123',
      remember: true,
    });

    const request = httpMock.expectOne(`${API}/register`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.name).toBe('Новый Игрок');
    expect(request.request.body.email).toBe('player@gametrack.app');
    request.flush(SESSION);

    const session = await loading;

    expect(session.email).toBe('player@gametrack.app');
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.session()?.name).toBe('Новый Игрок');
  });

  it('преобразует конфликт email в ApiError со статусом 409', async () => {
    const loading = service.register({
      name: 'Новый Игрок',
      email: 'demo@gametrack.app',
      password: 'password123',
      remember: false,
    });

    const request = httpMock.expectOne(`${API}/register`);
    request.flush(
      { message: 'Аккаунт с таким email уже существует.' },
      { status: 409, statusText: 'Conflict' },
    );

    await expectAsync(loading).toBeRejectedWithError('Аккаунт с таким email уже существует.');
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('валидирует данные регистрации до запроса', async () => {
    await expectAsync(
      service.register({
        name: 'A',
        email: 'bad-email',
        password: 'short',
        remember: false,
      }),
    ).toBeRejected();

    httpMock.expectNone(`${API}/register`);
  });

  it('выполняет вход через POST и сохраняет сессию', async () => {
    const loading = service.login({
      email: 'demo@gametrack.app',
      password: 'demo1234',
      remember: true,
    });

    const request = httpMock.expectOne(`${API}/login`);
    expect(request.request.method).toBe('POST');
    request.flush({ ...SESSION, email: 'demo@gametrack.app', name: 'Алексей Игрок' });

    const session = await loading;

    expect(session.name).toBe('Алексей Игрок');
    expect(service.isAuthenticated()).toBeTrue();
  });
});
