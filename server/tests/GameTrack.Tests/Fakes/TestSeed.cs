using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;

namespace GameTrack.Tests.Fakes;

/// <summary>
/// Общие данные для тестов: демонстрационный профиль и семь игр.
/// Раньше этот класс лежал в Infrastructure (рабочий seed), теперь он
/// нужен только тестам: боевые данные создаются скриптами db/*.sql.
/// </summary>
public static class TestSeed
{
    public static readonly Guid ProfileId = Guid.Parse("2f7a1c1e-6d40-4fbc-9d1f-3f2f7a9b6e10");

    public static readonly string Login = "demo";

    private static readonly DateTimeOffset ProfileCreatedAt =
        new(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static UserProfile CreateProfile() => UserProfile.Restore(
        ProfileId,
        "Алексей Игрок",
        "demo@gametrack.app",
        "images/avatar.svg",
        ProfileCreatedAt,
        ProfileCreatedAt);

    public static IReadOnlyList<Game> CreateGames() =>
    [
        Game.Restore(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "The Witcher 3: Wild Hunt",
            "RPG",
            "PC",
            GameStatus.Completed,
            "images/covers/witcher-3.svg",
            "Ролевая игра о ведьмаке Геральте из Ривии, который ищет Цири и сражается с Дикой Охотой. Огромный открытый мир, десятки побочных заданий и вариативные решения.",
            10m,
            2015,
            new DateTimeOffset(2024, 1, 5, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 10, 18, 30, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "Red Dead Redemption 2",
            "Action",
            "PlayStation 4",
            GameStatus.InProgress,
            "images/covers/rdr2.svg",
            "История банды Ван дер Линде на закате эпохи Дикого Запада. Медленное повествование, детализированный мир и один из самых проработанных открытых миров.",
            9.5m,
            2018,
            new DateTimeOffset(2024, 2, 1, 15, 20, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 4, 2, 20, 10, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "Cyberpunk 2077",
            "RPG",
            "PC",
            GameStatus.InProgress,
            "images/covers/cyberpunk-2077.svg",
            "Приключение в Найт-Сити — городе будущего, где технологии изменили всё. Свобода в билдах персонажа, шутер и ролевая система в одном.",
            8.5m,
            2020,
            new DateTimeOffset(2024, 2, 14, 10, 5, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 4, 18, 9, 40, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("44444444-4444-4444-8444-444444444444"),
            "Hogwarts Legacy",
            "Adventure",
            "PlayStation 5",
            GameStatus.WantToPlay,
            "images/covers/hogwarts-legacy.svg",
            "Магическое приключение в Хогвартсе XIX века. Уроки, зелья, заклинания и исследование замка и его окрестностей.",
            null,
            2023,
            new DateTimeOffset(2024, 3, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 1, 8, 0, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("55555555-5555-4555-8555-555555555555"),
            "Stardew Valley",
            "Simulation",
            "Nintendo Switch",
            GameStatus.Dropped,
            "images/covers/stardew-valley.svg",
            "Уютный симулятор фермерства: выращивайте урожай, разводите животных, заводите друзей и исследуйте долину.",
            7m,
            2016,
            new DateTimeOffset(2024, 1, 20, 19, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 2, 25, 21, 0, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("66666666-6666-4666-8666-666666666666"),
            "Elden Ring",
            "Action",
            "PC",
            GameStatus.Completed,
            "images/covers/elden-ring.svg",
            "Тёмное фэнтези от FromSoftware: открытый мир Междуземья, сложные боссы и исследование, построенное на любопытстве.",
            9m,
            2022,
            new DateTimeOffset(2024, 2, 20, 13, 45, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 5, 22, 20, 0, TimeSpan.Zero)),
        Game.Restore(
            Guid.Parse("77777777-7777-4777-8777-777777777777"),
            "Hollow Knight",
            "Platformer",
            "PC",
            GameStatus.WantToPlay,
            "images/covers/hollow-knight.svg",
            "Метроидвания о рыцаре, исследующем подземное королевство Халлоунест. Точное управление, ручная рисовка и непростая боёвка.",
            null,
            2017,
            new DateTimeOffset(2024, 3, 12, 11, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 12, 11, 30, 0, TimeSpan.Zero))
    ];
}
