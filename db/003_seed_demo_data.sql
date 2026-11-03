-- =============================================================================
-- GameTrack — демонстрационные данные
-- 003_seed_demo_data.sql
--
-- Создаёт демо-аккаунт demo@gametrack.app и семь игр его библиотеки —
-- те же данные, что раньше отдавал SeedData из in-memory репозитория.
--
-- Пароль демо-аккаунта: demo1234
-- Хеш: PBKDF2-HMAC-SHA256, 100 000 итераций, соль 16 байт, ключ 32 байта.
-- Формат хранения: pbkdf2-sha256$<итерации>$<соль base64>$<хеш base64>
--
-- Скрипт идемпотентен: если демо-аккаунт уже есть, данные не дублируются.
--
-- Запуск (после 001 и 002):
--   psql -h localhost -p 5432 -U postgres -d gametrack -f db/003_seed_demo_data.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Демо-аккаунт и профиль.
-- -----------------------------------------------------------------------------
INSERT INTO accounts (login, email, password_hash)
VALUES (
    'demo',
    'demo@gametrack.app',
    'pbkdf2-sha256$100000$AQIDBAUGBwgJCgsMDQ4PEA==$e/NCueltBBSvHBHLWewhYQcCd8+17U6p9jiqFYYFrJg='
)
ON CONFLICT (email) DO NOTHING;

INSERT INTO user_profiles (account_id, display_name, avatar_url)
SELECT id, 'Алексей Игрок', 'images/avatar.svg'
FROM accounts
WHERE email = 'demo@gametrack.app'
ON CONFLICT (account_id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 2. Библиотека демо-пользователя.
-- Вставляем игры только если библиотека ещё пуста, и только по одному разу.
-- -----------------------------------------------------------------------------
CREATE TEMP TABLE seed_games (
    title        varchar(200)  PRIMARY KEY,
    status_code  varchar(30)   NOT NULL,
    rating       numeric(3, 1),
    release_year smallint,
    cover_url    varchar(500),
    description  text,
    genre_name   varchar(50)   NOT NULL,
    platform_name varchar(50)  NOT NULL,
    created_at   timestamptz   NOT NULL,
    updated_at   timestamptz   NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_games VALUES
    ('The Witcher 3: Wild Hunt', 'Completed', 10.0, 2015,
     'images/covers/witcher-3.svg',
     'Ролевая игра о ведьмаке Геральте из Ривии, который ищет Цири и сражается с Дикой Охотой. Огромный открытый мир, десятки побочных заданий и вариативные решения.',
     'RPG', 'PC',
     timestamptz '2024-01-05 12:00:00+00', timestamptz '2024-03-10 18:30:00+00'),
    ('Red Dead Redemption 2', 'Playing', 9.5, 2018,
     'images/covers/rdr2.svg',
     'История банды Ван дер Линде на закате эпохи Дикого Запада. Медленное повествование, детализированный мир и один из самых проработанных открытых миров.',
     'Action', 'PlayStation 4',
     timestamptz '2024-02-01 15:20:00+00', timestamptz '2024-04-02 20:10:00+00'),
    ('Cyberpunk 2077', 'Playing', 8.5, 2020,
     'images/covers/cyberpunk-2077.svg',
     'Приключение в Найт-Сити — городе будущего, где технологии изменили всё. Свобода в билдах персонажа, шутер и ролевая система в одном.',
     'RPG', 'PC',
     timestamptz '2024-02-14 10:05:00+00', timestamptz '2024-04-18 09:40:00+00'),
    ('Hogwarts Legacy', 'Planned', NULL, 2023,
     'images/covers/hogwarts-legacy.svg',
     'Магическое приключение в Хогвартсе XIX века. Уроки, зелья, заклинания и исследование замка и его окрестностей.',
     'Adventure', 'PlayStation 5',
     timestamptz '2024-03-01 08:00:00+00', timestamptz '2024-03-01 08:00:00+00'),
    ('Stardew Valley', 'Dropped', 7.0, 2016,
     'images/covers/stardew-valley.svg',
     'Уютный симулятор фермерства: выращивайте урожай, разводите животных, заводите друзей и исследуйте долину.',
     'Simulation', 'Nintendo Switch',
     timestamptz '2024-01-20 19:15:00+00', timestamptz '2024-02-25 21:00:00+00'),
    ('Elden Ring', 'Completed', 9.0, 2022,
     'images/covers/elden-ring.svg',
     'Тёмное фэнтези от FromSoftware: открытый мир Междуземья, сложные боссы и исследование, построенное на любопытстве.',
     'Action', 'PC',
     timestamptz '2024-02-20 13:45:00+00', timestamptz '2024-05-05 22:20:00+00'),
    ('Hollow Knight', 'Planned', NULL, 2017,
     'images/covers/hollow-knight.svg',
     'Метроидвания о рыцаре, исследующем подземное королевство Халлоунест. Точное управление, ручная рисовка и непростая боёвка.',
     'Platformer', 'PC',
     timestamptz '2024-03-12 11:30:00+00', timestamptz '2024-03-12 11:30:00+00');

-- Справочники для платформ, которые встречаются в демо-наборе.
INSERT INTO platforms (name)
SELECT DISTINCT platform_name FROM seed_games
ON CONFLICT (name) DO NOTHING;

INSERT INTO genres (name)
SELECT DISTINCT genre_name FROM seed_games
ON CONFLICT (name) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 3. Сами игры, их жанры и платформы.
-- -----------------------------------------------------------------------------
INSERT INTO games (account_id, title, cover_url, description, status_id, rating, release_year, created_at, updated_at)
SELECT a.id, s.title, s.cover_url, s.description, st.id, s.rating, s.release_year, s.created_at, s.updated_at
FROM seed_games AS s
CROSS JOIN accounts AS a
JOIN game_statuses AS st ON st.code = s.status_code
WHERE a.email = 'demo@gametrack.app'
  AND NOT EXISTS (
      SELECT 1 FROM games AS g
      WHERE g.account_id = a.id AND lower(g.title) = lower(s.title)
  );

INSERT INTO game_genres (game_id, genre_id)
SELECT g.id, ge.id
FROM seed_games AS s
CROSS JOIN accounts AS a
JOIN games AS g
    ON g.account_id = a.id
   AND lower(g.title) = lower(s.title)
JOIN genres AS ge ON ge.name = s.genre_name
WHERE a.email = 'demo@gametrack.app'
ON CONFLICT DO NOTHING;

INSERT INTO game_platforms (game_id, platform_id)
SELECT g.id, p.id
FROM seed_games AS s
CROSS JOIN accounts AS a
JOIN games AS g
    ON g.account_id = a.id
   AND lower(g.title) = lower(s.title)
JOIN platforms AS p ON p.name = s.platform_name
WHERE a.email = 'demo@gametrack.app'
ON CONFLICT DO NOTHING;

COMMIT;
