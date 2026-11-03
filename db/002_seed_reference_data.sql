-- =============================================================================
-- GameTrack — справочные данные (жанры, платформы, статусы)
-- 002_seed_reference_data.sql
--
-- Заполняет справочники, без которых приложение не сможет создавать игры:
--   game_statuses — коды статусов, совпадающие с enum GameStatus в Domain;
--   genres        — список жанров;
--   platforms     — список платформ.
--
-- Скрипт идемпотентен: повторный запуск не создаёт дублей.
--
-- Запуск:
--   psql -h localhost -p 5432 -U postgres -d gametrack -f db/002_seed_reference_data.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Статусы игры.
-- ВАЖНО: id совпадает с числовым значением enum GameStatus (Domain/Enums/GameStatus.cs),
-- поэтому EF Core может маппить enum на этот справочник без промежуточной таблицы.
--   GameStatus.InProgress  = 1 -> 'Playing'    «В процессе»
--   GameStatus.Completed   = 2 -> 'Completed'  «Пройдено»
--   GameStatus.WantToPlay  = 3 -> 'Planned'    «Хочу пройти»
--   GameStatus.Dropped     = 4 -> 'Dropped'    «Брошено»
--
-- Для существующей БД (igri) нумерация другая — её приводит
-- db/004_align_existing_schema.sql, который запускается ПОСЛЕ этого файла.
--
-- Коды (code) уникальны, поэтому коды не могут меняться местами между id
-- в одном UPDATE: при повторном запуске на уже перестроенной БД ничего
-- не меняем. Для существующей БД id/коды остаются как есть (не трогаем
-- данные пользователя), сверка выполняется в 004.
-- -----------------------------------------------------------------------------
INSERT INTO game_statuses (id, code, name)
SELECT v.id, v.code, v.name
FROM (VALUES
    (1::smallint, 'Playing',   'В процессе'),
    (2::smallint, 'Completed', 'Пройдено'),
    (3::smallint, 'Planned',   'Хочу пройти'),
    (4::smallint, 'Dropped',   'Брошено')
) AS v (id, code, name)
WHERE NOT EXISTS (SELECT 1 FROM game_statuses)
ON CONFLICT (id) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Жанры.
-- -----------------------------------------------------------------------------
INSERT INTO genres (name) VALUES
    ('RPG'),
    ('Action'),
    ('Adventure'),
    ('Strategy'),
    ('Simulation'),
    ('Indie')
ON CONFLICT (name) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Платформы.
-- Имена должны совпадать со значениями, которые присылает клиент в поле platform.
-- -----------------------------------------------------------------------------
INSERT INTO platforms (name) VALUES
    ('PC'),
    ('PlayStation'),
    ('Xbox'),
    ('Nintendo Switch')
ON CONFLICT (name) DO NOTHING;

COMMIT;
