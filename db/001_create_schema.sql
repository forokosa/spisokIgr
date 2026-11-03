-- =============================================================================
-- GameTrack — схема базы данных PostgreSQL (лабораторная работа №4)
-- 001_create_schema.sql
--
-- Источник истины для схемы. Скрипт приведён к третьей нормальной форме (3НФ):
--   * каждый факт хранится в одном месте, без дублирования строковых справочников
--     в таблице игр (жанры и платформы вынесены в отдельные справочники и
--     связаны с играми через таблицы связей many-to-many);
--   * все технические ключи — bigint GENERATED ALWAYS AS IDENTITY;
--   * на столбцах внешних ключей (кроме ведущих в составных первичных ключах)
--     созданы индексы.
--
-- Скрипт идемпотентен (безопасен для повторного запуска) и НЕ содержит
-- миграций Entity Framework: приложение работает по модели "database first"
-- и не использует EnsureCreated()/Migrate().
--
-- Запуск:
--   psql -h localhost -p 5432 -U postgres -d gametrack -f db/001_create_schema.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Справочник статусов игры.
--   id   — небольшой код (smallint), на который ссылается games.status_id;
--   code — стабильный код для приложения (совпадает с GameStatus в Domain);
--   name — отображаемое название для клиента.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS game_statuses (
    id   smallint     NOT NULL,
    code varchar(30)  NOT NULL,
    name varchar(50)  NOT NULL,
    CONSTRAINT game_statuses_pkey     PRIMARY KEY (id),
    CONSTRAINT game_statuses_code_key UNIQUE (code)
);

-- -----------------------------------------------------------------------------
-- Справочник жанров.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS genres (
    id   bigint      NOT NULL GENERATED ALWAYS AS IDENTITY,
    name varchar(50) NOT NULL,
    CONSTRAINT genres_pkey     PRIMARY KEY (id),
    CONSTRAINT genres_name_key UNIQUE (name)
);

-- -----------------------------------------------------------------------------
-- Справочник платформ.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS platforms (
    id   bigint      NOT NULL GENERATED ALWAYS AS IDENTITY,
    name varchar(50) NOT NULL,
    CONSTRAINT platforms_pkey     PRIMARY KEY (id),
    CONSTRAINT platforms_name_key UNIQUE (name)
);

-- -----------------------------------------------------------------------------
-- Учётные записи. Пароль хранится только как хеш PBKDF2-SHA256 (см. README).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS accounts (
    id            bigint       NOT NULL GENERATED ALWAYS AS IDENTITY,
    login         varchar(50)  NOT NULL,
    email         varchar(255) NOT NULL,
    password_hash varchar(255) NOT NULL,
    created_at    timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT accounts_pkey      PRIMARY KEY (id),
    CONSTRAINT accounts_login_key UNIQUE (login),
    CONSTRAINT accounts_email_key UNIQUE (email)
);

-- -----------------------------------------------------------------------------
-- Профили пользователей. Один аккаунт — один профиль.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_profiles (
    id           bigint       NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id   bigint       NOT NULL,
    display_name varchar(100) NOT NULL,
    avatar_url   varchar(500),
    about        varchar(1000),
    updated_at   timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT user_profiles_pkey           PRIMARY KEY (id),
    CONSTRAINT user_profiles_account_id_key UNIQUE (account_id),
    CONSTRAINT user_profiles_account_id_fkey
        FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE CASCADE
);

-- -----------------------------------------------------------------------------
-- Игры в личной библиотеке пользователя.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS games (
    id           bigint        NOT NULL GENERATED ALWAYS AS IDENTITY,
    account_id   bigint        NOT NULL,
    title        varchar(200)  NOT NULL,
    cover_url    varchar(500),
    description  text,
    status_id    smallint      NOT NULL,
    rating       numeric(3, 1),
    hours_played numeric(7, 1) NOT NULL DEFAULT 0,
    release_year smallint,
    created_at   timestamptz   NOT NULL DEFAULT now(),
    updated_at   timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT games_pkey              PRIMARY KEY (id),
    CONSTRAINT games_account_id_fkey   FOREIGN KEY (account_id) REFERENCES accounts (id)       ON DELETE CASCADE,
    CONSTRAINT games_status_id_fkey    FOREIGN KEY (status_id)  REFERENCES game_statuses (id),
    CONSTRAINT games_rating_check      CHECK (rating IS NULL OR (rating >= 0.0 AND rating <= 10.0)),
    CONSTRAINT games_hours_played_check CHECK (hours_played >= 0)
);

-- -----------------------------------------------------------------------------
-- Связь игр с жанрами (many-to-many).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS game_genres (
    game_id  bigint NOT NULL,
    genre_id bigint NOT NULL,
    CONSTRAINT game_genres_pkey        PRIMARY KEY (game_id, genre_id),
    CONSTRAINT game_genres_game_id_fkey
        FOREIGN KEY (game_id)  REFERENCES games (id)   ON DELETE CASCADE,
    CONSTRAINT game_genres_genre_id_fkey
        FOREIGN KEY (genre_id) REFERENCES genres (id)  ON DELETE RESTRICT
);

-- -----------------------------------------------------------------------------
-- Связь игр с платформами (many-to-many).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS game_platforms (
    game_id     bigint NOT NULL,
    platform_id bigint NOT NULL,
    CONSTRAINT game_platforms_pkey        PRIMARY KEY (game_id, platform_id),
    CONSTRAINT game_platforms_game_id_fkey
        FOREIGN KEY (game_id)     REFERENCES games (id)      ON DELETE CASCADE,
    CONSTRAINT game_platforms_platform_id_fkey
        FOREIGN KEY (platform_id) REFERENCES platforms (id)  ON DELETE RESTRICT
);

-- -----------------------------------------------------------------------------
-- Индексы на внешних ключах (ведущий столбец составного PK уже покрыт
-- первичным ключом, поэтому индексируются только вторые столбцы связей и
-- внешние ключи обычных таблиц).
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS ix_games_account_id      ON games (account_id);
CREATE INDEX IF NOT EXISTS ix_games_status_id       ON games (status_id);
CREATE INDEX IF NOT EXISTS ix_games_account_title   ON games (account_id, title);
CREATE INDEX IF NOT EXISTS ix_game_genres_genre_id  ON game_genres (genre_id);
CREATE INDEX IF NOT EXISTS ix_game_platforms_platform ON game_platforms (platform_id);

-- Название игры уникально внутри библиотеки пользователя (без учёта регистра).
CREATE UNIQUE INDEX IF NOT EXISTS ux_games_account_title_ci ON games (account_id, lower(title));

-- Поиск аккаунта по публичному Guid: длина логина закодирована в идентификаторе
-- (см. AccountIdMap), поэтому выборка сужается этим индексом.
CREATE INDEX IF NOT EXISTS ix_accounts_login_length ON accounts (length(lower(login)));

COMMIT;
