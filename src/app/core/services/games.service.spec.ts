import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { GamesService } from './games.service';

describe('GamesService', () => {
  let service: GamesService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(GamesService);
  });

  it('загружает библиотеку из mock-слоя', fakeAsync(() => {
    expect(service.loaded()).toBeFalse();
    void service.load();
    tick(1000);
    expect(service.loaded()).toBeTrue();
    expect(service.games().length).toBeGreaterThanOrEqual(5);
    expect(service.loading()).toBeFalse();
  }));

  it('изменяет статус и обновляет локальный кэш', fakeAsync(() => {
    void service.load();
    tick(1000);
    const target = service.games()[0];
    void service.changeStatus(target.id, 'dropped');
    tick(1000);
    expect(service.findById(target.id)?.status).toBe('dropped');
  }));

  it('удаляет игру из локального кэша', fakeAsync(() => {
    void service.load();
    tick(1000);
    const target = service.games()[0];
    const before = service.games().length;
    void service.remove(target.id);
    tick(1000);
    expect(service.games().length).toBe(before - 1);
    expect(service.findById(target.id)).toBeUndefined();
  }));

  it('фильтрует по поиску и статусу независимо', fakeAsync(() => {
    void service.load();
    tick(1000);
    service.setStatus('completed');
    service.setSearch('witcher');
    expect(service.query()).toEqual({ status: 'completed', search: 'witcher' });
    expect(service.filteredGames().every((game) => game.status === 'completed')).toBeTrue();
  }));
});
