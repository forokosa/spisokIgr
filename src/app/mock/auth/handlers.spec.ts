import { createSeedDatabase } from '../seed';
import { MockApiError } from '../mock-error';
import { loginHandler, requestRecoveryHandler } from './handlers';

describe('auth handlers', () => {
  it('выполняет вход при корректном email и непустом пароле', () => {
    const db = createSeedDatabase();
    const session = loginHandler(db, { email: 'user@example.com', password: 'secret', remember: true });
    expect(session.email).toBe('user@example.com');
    expect(session.userId).toBe(db.profile.id);
    expect(session.remember).toBeTrue();
  });

  it('отклоняет некорректный email', () => {
    const db = createSeedDatabase();
    expect(() => loginHandler(db, { email: 'bad', password: 'secret', remember: false })).toThrowError(
      MockApiError,
    );
  });

  it('отклоняет пустой пароль', () => {
    const db = createSeedDatabase();
    expect(() =>
      loginHandler(db, { email: 'user@example.com', password: '  ', remember: false }),
    ).toThrowError(MockApiError);
  });

  it('подтверждает запрос восстановления', () => {
    const db = createSeedDatabase();
    const result = requestRecoveryHandler(db, 'user@example.com');
    expect(result.delivered).toBeTrue();
    expect(result.code.length).toBe(6);
  });
});
