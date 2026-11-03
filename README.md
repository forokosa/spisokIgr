# GameTrack — лабораторная работа №4

Приложение для учёта игровой библиотеки: Angular-клиент + ASP.NET Core Web API,
построенное по **Domain-Driven Design**. Начиная с этой работы данные хранятся в
**PostgreSQL**: половина репозиториев работает через чистый SQL (Npgsql), половина —
через **Entity Framework Core** в режиме database first.

## Структура репозитория

```
spisokIgr/
├── client/                         # Angular 20 SPA
│   └── src/app/
│       ├── core/                   # HTTP-сервисы, guard'ы, конфигурация
│       ├── features/               # Страницы: игры, желаемое, статистика, профиль, auth
│       └── shared/                 # Модели, компоненты, утилиты, валидаторы
├── db/                             # SQL-скрипты схемы и данных (источник истины)
├── docs/
│   └── db-schema.md                # ER-диаграмма и описание схемы
└── server/
    ├── GameTrack.slnx
    ├── src/
    │   ├── GameTrack.Domain/       # Сущности, value-объекты, интерфейсы, доменные сервисы
    │   ├── GameTrack.Infrastructure/ # Npgsql-репозитории, EF Core-контекст, конфигурации
    │   └── GameTrack.Api/          # Контроллеры, DTO, middleware, Program.cs
    └── tests/
        └── GameTrack.Tests/        # xUnit: Domain, Infrastructure, Presentation, Integration
```

## Архитектура DDD

- **Domain** — сущности `Game` и `UserProfile`, статусы, `PagedResult`, интерфейсы
  `IGameRepository` / `IUserProfileRepository` / `IAccountRepository`, доменные сервисы
  `GameService`, `StatisticsService`, `ProfileService`, `AuthService`.
  Вся валидация входных данных и бизнес-правила находятся здесь.
  Domain **не ссылается** на EF Core и Npgsql.
- **Infrastructure** — реализации репозиториев поверх PostgreSQL и регистрация DI
  (`AddPersistence`). Единственный слой, знающий про `NpgsqlDataSource` и `DbContext`.
- **Presentation** — тонкие контроллеры только с CRUD, DTO Request/Response,
  глобальный middleware исключений, middleware логирования и middleware,
  определяющий текущего пользователя по заголовку `X-User-Id`.

## Доступ к данным

| Интерфейс | Технология | Реализация |
|---|---|---|
| `IAccountRepository` | чистый SQL (Npgsql) | `NpgsqlAccountRepository` |
| `IUserProfileRepository` | чистый SQL (Npgsql) | `NpgsqlUserProfileRepository` |
| `IGameRepository` (игры, жанры, платформы, статистика) | Entity Framework Core | `EfGameRepository` + `GameTrackDbContext` |

Правила обеих групп реализаций:

- только параметризованные запросы, без конкатенации значений в SQL;
- никаких `SELECT *` — столбцы перечисляются явно;
- все методы асинхронные, принимают `CancellationToken`;
- SQL-репозитории берут соединение у `NpgsqlDataSource` через `await using`,
  а нарушение уникальности (`PostgresException`, SQLSTATE `23505`) превращается
  в доменный `ConflictException`;
- EF-запросы используют `AsNoTracking`, `Select` нужных полей, а фильтрация и
  пагинация выполняются на стороне БД (`Where` / `Skip` / `Take`); `Include` не используется;
- **каждая** операция с играми и профилем ограничена аккаунтом текущего
  пользователя: `X-User-Id`, чужой идентификатор не попадает в запрос, чужая
  игра возвращает `404`, чужая библиотека — пустую страницу.

Enum `GameStatus` сопоставлен справочнику `game_statuses` через явный
value-конвертер (`GameStatusConverter`), а идентификаторы `Guid` в API
детерминированно отображаются в `bigint`-ключи БД (`AccountIdMap`, `GameKey`).
Подробности — в [`docs/db-schema.md`](docs/db-schema.md).

## Асинхронность и пагинация

- Все методы репозиториев, сервисов, контроллеров и тестов — `async` / `await`.
- Коллекции возвращаются пачками: `GET /api/games?page=1&pageSize=6&search=&status=`.
- Параметры пагинации валидируются и на клиенте, и в контроллере, и в Domain.
- Клиент использует `HttpClient` и после каждого запроса продолжает цепочку `.pipe(...)`
  (`tap`, `catchError`).

## База данных

Схема приведена к 3НФ и описана в [`docs/db-schema.md`](docs/db-schema.md).
Скрипты пронумерованы и лежат в [`db/`](db).

### 1. Создать базу

```bash
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE gametrack WITH ENCODING 'UTF8' TEMPLATE template0"
```

База должна быть в UTF-8: в данных есть русский текст (профиль, описания игр).

### 2. Выполнить скрипты в порядке номеров

```bash
psql -h localhost -p 5432 -U postgres -d gametrack -v ON_ERROR_STOP=1 -f db/001_create_schema.sql
psql -h localhost -p 5432 -U postgres -d gametrack -v ON_ERROR_STOP=1 -f db/002_seed_reference_data.sql
psql -h localhost -p 5432 -U postgres -d gametrack -v ON_ERROR_STOP=1 -f db/003_seed_demo_data.sql
```

| Скрипт | Что делает |
|---|---|
| `001_create_schema.sql` | создаёт таблицы, ограничения и индексы (скрипт идемпотентен) |
| `002_seed_reference_data.sql` | заполняет справочники: статусы, жанры, платформы |
| `003_seed_demo_data.sql` | создаёт демо-аккаунт и семь игр его библиотеки |
| `004_align_existing_schema.sql` | **только** для БД, созданной до этой лабораторной: приводит коды статусов, дробный рейтинг и уникальность названий к требованиям приложения |

Для чистой установки достаточно файлов `001`–`003`. Скрипт `004` нужен, если у вас
уже есть рабочая база (например, `igri`) — он выполняется последним и не пересоздаёт схему.

### 3. Задать строку подключения через User Secrets

В `appsettings.json` хранится только шаблон без пароля; реальная строка подключается
через `dotnet user-secrets` (файл вне репозитория):

```bash
cd server/src/GameTrack.Api
dotnet user-secrets set "ConnectionStrings:GameTrack" "Host=localhost;Port=5432;Database=gametrack;Username=postgres;Password=<ваш пароль>"
```

Проверить, что секрет на месте:

```bash
dotnet user-secrets list
```

Альтернатива для локальной отладки — файл `appsettings.Local.json` в папке
`GameTrack.Api` (исключён из git через `.gitignore`).

### 4. Запустить backend

```bash
cd server
dotnet run --project src/GameTrack.Api
```

- API: `http://localhost:5080/api`
- Swagger UI: `http://localhost:5080/swagger`

Демо-учётные данные: `demo@gametrack.app` / `demo1234` (создаются скриптом
`003_seed_demo_data.sql`). Новые аккаунты сохраняются в PostgreSQL, у каждого
аккаунта своя библиотека: демо-профиль стартует с семью играми, новый аккаунт —
с пустым списком.

> Приложение не вызывает `EnsureCreated()`/`Migrate()` и не изменяет схему:
> если таблицы не созданы, запросы завершатся ошибкой БД. Сначала выполните скрипты.

### Тестовая база

Интеграционные тесты создают временную базу с префиксом `gametrack_tests_`
(по имени скриптов `db/001`–`db/003` из неё), запускают проверки и удаляют её.
Адрес сервера задаётся переменной `TEST_DB_CONNECTION` (по умолчанию — `postgres@localhost`):

```bash
set TEST_DB_CONNECTION=Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<пароль>
dotnet test server/GameTrack.slnx
```

Если PostgreSQL недоступен, интеграционные тесты помечаются как пропущенные,
а юнит-тесты выполняются как обычно.

## Frontend

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

Покрыто:

- **Domain** — инварианты сущностей, все публичные методы сервисов, «злые» входные данные;
- **Infrastructure** — изоляция библиотек по пользователю, конвертеры статусов;
- **Presentation** — контроллеры и коды `200 / 201 / 204 / 400 / 401 / 404 / 409`, middleware;
- **Integration** — SQL-репозитории аккаунтов и профиля и EF-репозиторий игр на реальном
  PostgreSQL: фильтрация и пагинация в БД, связи жанров и платформ, изоляция по пользователю;
- **Client** — асинхронные HTTP-вызовы, query-параметры, обработка ошибок.

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
dotnet user-secrets set "ConnectionStrings:GameTrack" "Host=localhost;Port=5432;Database=gametrack;Username=postgres;Password=<пароль>"
```

Пароли пользователей хранятся только в виде PBKDF2-SHA256-хешей
(`pbkdf2-sha256$итерации$соль$хеш`), в открытом виде — нигде.
