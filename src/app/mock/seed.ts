import { Game, UserProfile } from '../shared/models';
import { MockDatabase, MOCK_DB_VERSION } from './mock-db.model';

const SEED_PROFILE: UserProfile = {
  id: 'user_demo',
  name: 'Алексей Игрок',
  email: 'demo@gametrack.app',
  avatarUrl: 'images/avatar.svg',
  createdAt: '2024-01-01T09:00:00.000Z',
  updatedAt: '2024-01-01T09:00:00.000Z',
};

const SEED_GAMES: Game[] = [
  {
    id: 'game_witcher_3',
    title: 'The Witcher 3: Wild Hunt',
    genre: 'RPG',
    platform: 'PC',
    status: 'completed',
    coverUrl: 'images/covers/witcher-3.svg',
    description:
      'Ролевая игра о ведьмаке Геральте из Ривии, который ищет Цири и сражается с Дикой Охотой. Огромный открытый мир, десятки побочных заданий и вариативные решения.',
    rating: 10,
    releaseYear: 2015,
    addedAt: '2024-01-05T12:00:00.000Z',
    updatedAt: '2024-03-10T18:30:00.000Z',
  },
  {
    id: 'game_rdr2',
    title: 'Red Dead Redemption 2',
    genre: 'Action',
    platform: 'PlayStation 4',
    status: 'in-progress',
    coverUrl: 'images/covers/rdr2.svg',
    description:
      'История банды Ван дер Линде на закате эпохи Дикого Запада. Медленное повествование, детализированный мир и один из самых проработанных открытых миров.',
    rating: 9.5,
    releaseYear: 2018,
    addedAt: '2024-02-01T15:20:00.000Z',
    updatedAt: '2024-04-02T20:10:00.000Z',
  },
  {
    id: 'game_cyberpunk_2077',
    title: 'Cyberpunk 2077',
    genre: 'RPG',
    platform: 'PC',
    status: 'in-progress',
    coverUrl: 'images/covers/cyberpunk-2077.svg',
    description:
      'Приключение в Найт-Сити — городе будущего, где технологии изменили всё. Свобода в билдах персонажа, шутер и ролевая система в одном.',
    rating: 8.5,
    releaseYear: 2020,
    addedAt: '2024-02-14T10:05:00.000Z',
    updatedAt: '2024-04-18T09:40:00.000Z',
  },
  {
    id: 'game_hogwarts_legacy',
    title: 'Hogwarts Legacy',
    genre: 'Adventure',
    platform: 'PlayStation 5',
    status: 'want-to-play',
    coverUrl: 'images/covers/hogwarts-legacy.svg',
    description:
      'Магическое приключение в Хогвартсе XIX века. Уроки, зелья, заклинания и исследование замка и его окрестностей.',
    releaseYear: 2023,
    addedAt: '2024-03-01T08:00:00.000Z',
    updatedAt: '2024-03-01T08:00:00.000Z',
  },
  {
    id: 'game_stardew_valley',
    title: 'Stardew Valley',
    genre: 'Simulation',
    platform: 'Nintendo Switch',
    status: 'dropped',
    coverUrl: 'images/covers/stardew-valley.svg',
    description:
      'Уютный симулятор фермерства: выращивайте урожай, разводите животных, заводите друзей и исследуйте долину.',
    rating: 7,
    releaseYear: 2016,
    addedAt: '2024-01-20T19:15:00.000Z',
    updatedAt: '2024-02-25T21:00:00.000Z',
  },
  {
    id: 'game_elden_ring',
    title: 'Elden Ring',
    genre: 'Action',
    platform: 'PC',
    status: 'completed',
    coverUrl: 'images/covers/elden-ring.svg',
    description:
      'Тёмное фэнтези от FromSoftware: открытый мир Междуземья, сложные боссы и исследование, построенное на любопытстве.',
    rating: 9,
    releaseYear: 2022,
    addedAt: '2024-02-20T13:45:00.000Z',
    updatedAt: '2024-05-05T22:20:00.000Z',
  },
  {
    id: 'game_hollow_knight',
    title: 'Hollow Knight',
    genre: 'Platformer',
    platform: 'PC',
    status: 'want-to-play',
    coverUrl: 'images/covers/hollow-knight.svg',
    description:
      'Метроидвания о рыцаре, исследующем подземное королевство Халлоунест. Точное управление, ручная рисовка и непростая боёвка.',
    releaseYear: 2017,
    addedAt: '2024-03-12T11:30:00.000Z',
    updatedAt: '2024-03-12T11:30:00.000Z',
  },
];

/** Возвращает свежую копию начальных данных приложения. */
export function createSeedDatabase(): MockDatabase {
  return {
    version: MOCK_DB_VERSION,
    games: SEED_GAMES.map((game) => ({ ...game })),
    profile: { ...SEED_PROFILE },
  };
}

/** Начальные демонстрационные учётные данные. */
export const SEED_CREDENTIALS = {
  email: SEED_PROFILE.email,
  password: 'demo1234',
} as const;
