# GameTrack — лабораторная работа №3

Приложение для учёта игровой библиотеки: Angular-клиент + ASP.NET Core Web API,
построенное по **Domain-Driven Design** с полной асинхронностью и in-memory заглушкой БД.

## Структура репозитория

```
spisokIgr/
├── client/                         # Angular 20 SPA
│   └── src/app/
│       ├── core/                   # HTTP-сервисы, guard'ы, конфигурация
│       ├── features/               # Страницы: игры, желаемое, статистика, профиль, auth
│       └── shared/                 # Модели, компоненты, утилиты, валидаторы
└── server/
    ├── GameTrack.slnx
    ├── src/
    │   ├── GameTrack.Domain/       # Сущности, value-объекты, интерфейсы, доменные сервисы
    │   ├── GameTrack.Infrastructure/ # In-memory репозитории (заглушки БД), seed
    │   └── GameTrack.Api/          # Контроллеры, DTO, middleware, Program.cs
    └── tests/
        └── GameTrack.Tests/        # xUnit: Domain, Infrastructure, Presentation
```

## Архитектура DDD

- **Domain** — сущности `Game` и `UserProfile`, статусы, `PagedResult`, интерфейсы
  `IGameRepository` / `IUserProfileRepository` / `IAccountRepository`, доменные сервисы
  `GameService`, `StatisticsService`, `ProfileService`, `AuthService`.
  Вся валидация входных данных и бизнес-правила находятся здесь.
- **Infrastructure** — `InMemoryGameRepository`, `InMemoryUserProfileRepository`,
  `InMemoryAccountRepository` (пароли хранятся как PBKDF2-хеши с солью), провайдер
  отдельных in-memory библиотек для каждого аккаунта, seed-данные.
- **Presentation** — тонкие контроллеры только с CRUD, DTO Request/Response,
  глобальный middleware исключений и middleware логирования.

## Асинхронность и пагинация

- Все методы репозиториев, сервисов, контроллеров и тестов — `async` / `await`.
- Коллекции возвращаются пачками: `GET /api/games?page=1&pageSize=6&search=&status=`.
- Параметры пагинации валидируются и на клиенте, и в контроллере, и в Domain.
- Клиент использует `HttpClient` и после каждого запроса продолжает цепочку `.pipe(...)`
  (`tap`, `catchError`).

## Запуск

### Backend

```bash
cd server
dotnet run --project src/GameTrack.Api
```

- API: `http://localhost:5080/api`
- Swagger UI: `http://localhost:5080/swagger`

Демо-учётные данные: `demo@gametrack.app` / `demo1234`
(пароль задаётся в `appsettings.Development.json`; в production используйте UserSecrets).
На странице входа также доступна регистрация нового аккаунта — он сохраняется в памяти
до перезапуска API. У каждого аккаунта своя библиотека: демо-профиль стартует с семью
играми, а новый аккаунт — с пустым списком.

### Frontend

```bash
cd client
npm install
npm start
```

Клиент доступен на `http://localhost:4200`, CORS для этого адреса настроен на backend.

## Тесты

```bash
dotnet test server/GameTrack.slnx
cd client && npm test
```

Покрыты:
- Domain: инварианты сущностей, все публичные методы сервисов, «злые» входные данные;
- Infrastructure: асинхронный CRUD, пагинация, проверка аргументов;
- Presentation: контроллеры и коды `200 / 201 / 204 / 400 / 401 / 404 / 409`, middleware;
- Client: асинхронные HTTP-вызовы, query-параметры, обработка ошибок.

## REST API

| Метод | Маршрут | Успех | Ошибки |
|---|---|---|---|
| GET | `/api/games?page&pageSize&search&status` | 200 | 400 |
| GET | `/api/games/{id}` | 200 | 400, 404 |
| POST | `/api/games` | 201 | 400, 409 |
| PUT | `/api/games/{id}` | 200 | 400, 404, 409 |
| PATCH | `/api/games/{id}/status` | 200 | 400, 404 |
| DELETE | `/api/games/{id}` | 204 | 400, 404 |
| GET | `/api/profile` | 200 | 404 |
| PUT | `/api/profile` | 200 | 400, 404 |
| GET | `/api/statistics` | 200 | — |
| POST | `/api/auth/register` | 201 | 400, 409 |
| POST | `/api/auth/login` | 200 | 400, 401 |
| POST | `/api/auth/recovery` | 200 | 400, 404 |

Клиент передаёт идентификатор аккаунта в заголовке `X-User-Id`, чтобы API вернул игры
именно этого пользователя. Без заголовка используется демо-профиль. Это упрощённая
идентификация для лабораторной работы, а не полноценная авторизация с JWT.

## Секреты

Реальные строки подключения и пароли **не хранятся в репозитории**.
Используются `dotnet user-secrets`, `appsettings.Local.json` / `appsettings.Secrets.json`
(исключены через `.gitignore`, `.cursorignore`, `.aiignore`).

```bash
cd server/src/GameTrack.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;User Id=...;Password=..."
dotnet user-secrets set "DemoAuth:Password" "..."
```
