-- =============================================================================
-- GameTrack — приведение существующей БД к требованиям приложения
-- 004_align_existing_schema.sql
--
-- Скрипт НЕ пересоздаёт схему 001 и ничего не удаляет безвозвратно: он точечно
-- исправляет расхождения между доменной моделью GameTrack и фактической
-- схемой рабочей базы (igri). Для чистой установки достаточно 001 + 002 + 003,
-- запускать 004 в этом случае не нужно (он обнаружит, что всё уже верно).
--
-- Что исправляется:
--   1) game_statuses.id не совпадает со значениями enum GameStatus.
--        факт: 1 Planned, 2 Playing, 3 Completed, 4 Dropped, 5 Wishlist
--        нужно: 1 Playing/InProgress, 2 Completed, 3 Planned/WantToPlay, 4 Dropped
--      Коды пересекаются, поэтому перестройка идёт через временный диапазон
--      (100+), чтобы не нарушить первичный ключ и уникальность code.
--      Перенос нейтрализует 'Wishlist' в 3 (WantToPlay) — оба означают
--      «хочу пройти», поэтому данные не теряются.
--   2) Платформы 'PlayStation 4' / 'PlayStation 5' не совпадают со справочником
--      'PlayStation' -> привязки игр переносятся на общий код.
--   3) games.rating smallint не хранит дробные оценки (9.5), которые допускает
--      Domain -> numeric(3,1).
--   4) Уникальность названия внутри библиотеки раньше проверялась только
--      в памяти -> уникальный индекс по (account_id, lower(title)).
--      Существующие дубли переименовываются с суффиксом, а не удаляются.
--
-- Скрипт идемпотентен: повторный запуск безопасен.
--
-- Запуск:
--   psql -h localhost -p 5432 -U postgres -d igri -v ON_ERROR_STOP=1 \
--        -f db/004_align_existing_schema.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 0. Справочники должны существовать до переноса игр.
-- -----------------------------------------------------------------------------
INSERT INTO genres (name) VALUES
    ('RPG'), ('Action'), ('Adventure'), ('Strategy'), ('Simulation'), ('Indie')
ON CONFLICT (name) DO NOTHING;

INSERT INTO platforms (name) VALUES
    ('PC'), ('PlayStation'), ('Xbox'), ('Nintendo Switch')
ON CONFLICT (name) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 1. Платформы: 'PlayStation 4' / 'PlayStation 5' -> 'PlayStation'.
-- -----------------------------------------------------------------------------
CREATE TEMP TABLE ps_alias (old_id bigint) ON COMMIT DROP;

INSERT INTO ps_alias (old_id)
SELECT id FROM platforms WHERE name IN ('PlayStation 4', 'PlayStation 5');

INSERT INTO game_platforms (game_id, platform_id)
SELECT gp.game_id, p.id
FROM game_platforms AS gp
JOIN ps_alias AS a ON a.old_id = gp.platform_id
JOIN platforms AS p ON p.name = 'PlayStation'
ON CONFLICT DO NOTHING;

DELETE FROM game_platforms WHERE platform_id IN (SELECT old_id FROM ps_alias);
DELETE FROM platforms WHERE id IN (SELECT old_id FROM ps_alias);

-- -----------------------------------------------------------------------------
-- 2. Статусы: id должны совпадать со значениями enum GameStatus.
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    desired_playing   smallint;
    desired_completed smallint;
    desired_planned   smallint;
    desired_dropped   smallint;
BEGIN
    SELECT id INTO desired_playing   FROM game_statuses WHERE code IN ('Playing', 'InProgress')  LIMIT 1;
    SELECT id INTO desired_completed FROM game_statuses WHERE code = 'Completed'                 LIMIT 1;
    SELECT id INTO desired_planned   FROM game_statuses WHERE code IN ('Planned', 'WantToPlay')  LIMIT 1;
    SELECT id INTO desired_dropped   FROM game_statuses WHERE code = 'Dropped'                   LIMIT 1;

    IF desired_playing = 1 AND desired_completed = 2 AND desired_planned = 3 AND desired_dropped = 4 THEN
        RAISE NOTICE 'Коды game_statuses уже соответствуют enum GameStatus — шаг 2 пропущен.';
        RETURN;
    END IF;

    RAISE NOTICE 'Перестройка game_statuses: Playing=%, Completed=%, Planned=%, Dropped=%',
        desired_playing, desired_completed, desired_planned, desired_dropped;

    -- Запоминаем целевое значение для каждой игры до любых изменений.
    CREATE TEMP TABLE IF NOT EXISTS game_status_map (game_id bigint PRIMARY KEY, status_id smallint) ON COMMIT DROP;

    INSERT INTO game_status_map (game_id, status_id)
    SELECT g.id,
           CASE
               WHEN g.status_id = desired_playing   THEN 1
               WHEN g.status_id = desired_completed THEN 2
               WHEN g.status_id = desired_planned   THEN 3
               WHEN g.status_id = desired_dropped   THEN 4
               ELSE 3
           END
    FROM games AS g;

    -- Внешний ключ снимем: его нельзя переставить без ON UPDATE, а первичный
    -- ключ справочника в процессе меняется дважды.
    ALTER TABLE games DROP CONSTRAINT IF EXISTS games_status_id_fkey;
    ALTER TABLE game_statuses DROP CONSTRAINT IF EXISTS game_statuses_code_key;

    -- Запасной диапазон, не пересекающийся ни с текущими, ни с итоговыми кодами.
    UPDATE game_statuses SET id = id + 100;

    INSERT INTO game_statuses (id, code, name) VALUES
        (1, 'Playing',   'В процессе'),
        (2, 'Completed', 'Пройдено'),
        (3, 'Planned',   'Хочу пройти'),
        (4, 'Dropped',   'Брошено');

    DELETE FROM game_statuses WHERE id > 100;

    UPDATE games AS g
    SET status_id = m.status_id
    FROM game_status_map AS m
    WHERE m.game_id = g.id;

    ALTER TABLE games ALTER COLUMN status_id SET NOT NULL;

    ALTER TABLE games
        ADD CONSTRAINT games_status_id_fkey
        FOREIGN KEY (status_id) REFERENCES game_statuses (id);

    ALTER TABLE game_statuses
        ADD CONSTRAINT game_statuses_code_key UNIQUE (code);
END $$;

-- -----------------------------------------------------------------------------
-- 3. Рейтинг: smallint -> numeric(3,1) (0.0..10.0).
-- -----------------------------------------------------------------------------
ALTER TABLE games
    ALTER COLUMN rating TYPE numeric(3, 1) USING rating::numeric(3, 1);

ALTER TABLE games DROP CONSTRAINT IF EXISTS games_rating_check;

ALTER TABLE games
    ADD CONSTRAINT games_rating_check CHECK (rating IS NULL OR (rating >= 0.0 AND rating <= 10.0));

-- -----------------------------------------------------------------------------
-- 4. Уникальность названия внутри библиотеки.
--    Существующие дубли не удаляем: переименовываем с суффиксом "(дубль N)"
--    и печатаем их список, чтобы владелец мог проверить данные.
-- -----------------------------------------------------------------------------
SELECT account_id, lower(title) AS title, count(*) AS duplicates
FROM games
GROUP BY account_id, lower(title)
HAVING count(*) > 1
ORDER BY account_id, title;

UPDATE games AS g
SET title = g.title || ' (дубль ' || r.position || ')'
FROM (
    SELECT id,
           row_number() OVER (PARTITION BY account_id, lower(title) ORDER BY id) AS position
    FROM games
) AS r
WHERE r.id = g.id
  AND r.position > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_games_account_title_ci
    ON games (account_id, lower(title));

COMMIT;
