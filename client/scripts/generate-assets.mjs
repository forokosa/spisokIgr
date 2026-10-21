import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';

const root = process.cwd();
const coversDir = join(root, 'public', 'images', 'covers');
mkdirSync(coversDir, { recursive: true });

const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

function cover({ file, title, subtitle, from, to, accent, monogram }) {
  const words = title.split(' ');
  const lines = [];
  let current = '';
  for (const word of words) {
    if ((current + ' ' + word).trim().length > 16) {
      lines.push(current.trim());
      current = word;
    } else {
      current = (current + ' ' + word).trim();
    }
  }
  if (current) lines.push(current);
  const textLines = lines
    .slice(0, 3)
    .map((line, i) => `<tspan x="46" dy="${i === 0 ? 0 : 46}">${esc(line)}</tspan>`)
    .join('');
  const baseY = 660 - (lines.slice(0, 3).length - 1) * 46;

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 800" preserveAspectRatio="xMidYMid slice" role="img" aria-label="${esc(title)}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${from}"/>
      <stop offset="1" stop-color="${to}"/>
    </linearGradient>
    <radialGradient id="glow" cx="0.8" cy="0.12" r="0.9">
      <stop offset="0" stop-color="${accent}" stop-opacity="0.55"/>
      <stop offset="1" stop-color="${accent}" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect width="600" height="800" fill="url(#bg)"/>
  <rect width="600" height="800" fill="url(#glow)"/>
  <circle cx="470" cy="150" r="190" fill="#ffffff" opacity="0.05"/>
  <circle cx="470" cy="150" r="120" fill="#ffffff" opacity="0.05"/>
  <path d="M0 560 C 160 500, 340 620, 600 520 L600 800 L0 800 Z" fill="#000000" opacity="0.28"/>
  <text x="46" y="150" font-family="Segoe UI, Arial, sans-serif" font-size="200" font-weight="800" fill="#ffffff" opacity="0.14">${esc(monogram)}</text>
  <text x="46" y="${baseY}" font-family="Segoe UI, Arial, sans-serif" font-size="44" font-weight="800" fill="#ffffff">${textLines}</text>
  <text x="46" y="${baseY + 54}" font-family="Segoe UI, Arial, sans-serif" font-size="22" font-weight="600" fill="#ffffff" opacity="0.75">${esc(subtitle)}</text>
</svg>
`;
  writeFileSync(join(coversDir, file), svg, 'utf8');
}

cover({ file: 'witcher-3.svg', title: 'The Witcher 3', subtitle: 'Wild Hunt · RPG', from: '#12332a', to: '#061119', accent: '#6fcf97', monogram: 'W' });
cover({ file: 'rdr2.svg', title: 'Red Dead Redemption 2', subtitle: 'Action · Western', from: '#5c2116', to: '#1a0a06', accent: '#e8845a', monogram: 'R' });
cover({ file: 'cyberpunk-2077.svg', title: 'Cyberpunk 2077', subtitle: 'RPG · Night City', from: '#2a0f5e', to: '#080418', accent: '#f7e04a', monogram: 'C' });
cover({ file: 'hogwarts-legacy.svg', title: 'Hogwarts Legacy', subtitle: 'Adventure · Magic', from: '#1a2c52', to: '#080f22', accent: '#e0b64a', monogram: 'H' });
cover({ file: 'stardew-valley.svg', title: 'Stardew Valley', subtitle: 'Simulation · Farming', from: '#2c6b34', to: '#0d2a16', accent: '#8fe07a', monogram: 'S' });
cover({ file: 'elden-ring.svg', title: 'Elden Ring', subtitle: 'Action · Dark Fantasy', from: '#5a4a1e', to: '#171103', accent: '#e8c766', monogram: 'E' });
cover({ file: 'hollow-knight.svg', title: 'Hollow Knight', subtitle: 'Platformer · Metroidvania', from: '#1c2450', to: '#080b20', accent: '#7fb0ff', monogram: 'H' });

const placeholder = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 800" preserveAspectRatio="xMidYMid slice" role="img" aria-label="Обложка недоступна">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#0e3153"/>
      <stop offset="1" stop-color="#071f35"/>
    </linearGradient>
  </defs>
  <rect width="600" height="800" fill="url(#bg)"/>
  <g fill="#4c6a90" transform="translate(300 380)">
    <rect x="-90" y="-60" width="180" height="130" rx="18" fill="none" stroke="#4c6a90" stroke-width="10"/>
    <circle cx="-42" cy="-4" r="16"/>
    <circle cx="42" cy="-4" r="16"/>
    <rect x="-70" y="90" width="140" height="16" rx="8" opacity="0.6"/>
  </g>
  <text x="300" y="620" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="30" font-weight="700" fill="#8fa6c4">Нет обложки</text>
</svg>
`;
writeFileSync(join(root, 'public', 'images', 'placeholder.svg'), placeholder, 'utf8');

const avatar = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" role="img" aria-label="Аватар пользователя">
  <defs>
    <linearGradient id="a" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#4c45ff"/>
      <stop offset="1" stop-color="#35c8ff"/>
    </linearGradient>
  </defs>
  <rect width="256" height="256" rx="64" fill="url(#a)"/>
  <circle cx="128" cy="100" r="46" fill="#071f35" opacity="0.85"/>
  <path d="M52 224c0-46 34-74 76-74s76 28 76 74z" fill="#071f35" opacity="0.85"/>
</svg>
`;
writeFileSync(join(root, 'public', 'images', 'avatar.svg'), avatar, 'utf8');

const scene = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 720 1000" preserveAspectRatio="xMidYMid slice" role="img" aria-label="Игровой пейзаж">
  <defs>
    <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#0b1f3a"/>
      <stop offset="0.55" stop-color="#0a2450"/>
      <stop offset="1" stop-color="#04121f"/>
    </linearGradient>
    <radialGradient id="neb" cx="0.3" cy="0.28" r="0.7">
      <stop offset="0" stop-color="#4c45ff" stop-opacity="0.55"/>
      <stop offset="1" stop-color="#4c45ff" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="ground" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#0a2036"/>
      <stop offset="1" stop-color="#04121f"/>
    </linearGradient>
  </defs>
  <rect width="720" height="1000" fill="url(#sky)"/>
  <rect width="720" height="1000" fill="url(#neb)"/>
  <circle cx="540" cy="180" r="70" fill="#cfe3ff" opacity="0.85"/>
  <g fill="#ffffff" opacity="0.5">
    <circle cx="90" cy="120" r="2"/><circle cx="180" cy="70" r="2"/><circle cx="300" cy="150" r="2"/>
    <circle cx="420" cy="90" r="2"/><circle cx="620" cy="120" r="2"/><circle cx="240" cy="240" r="2"/>
    <circle cx="660" cy="300" r="2"/><circle cx="120" cy="320" r="2"/>
  </g>
  <path d="M0 640 L140 520 L260 640 L380 470 L520 640 L620 560 L720 640 L720 1000 L0 1000 Z" fill="url(#ground)"/>
  <path d="M0 760 L180 660 L330 760 L470 680 L720 800 L720 1000 L0 1000 Z" fill="#061a2c"/>
  <!-- gamepad -->
  <g transform="translate(360 430)">
    <rect x="-120" y="-70" width="240" height="150" rx="60" fill="#0e3153" stroke="#4c45ff" stroke-width="4"/>
    <circle cx="-60" cy="0" r="20" fill="#35c8ff" opacity="0.9"/>
    <circle cx="60" cy="-18" r="14" fill="#9b7bff"/>
    <circle cx="88" cy="18" r="14" fill="#9b7bff"/>
    <rect x="-14" y="-8" width="28" height="10" rx="5" fill="#e8f0fb"/>
    <rect x="-5" y="-17" width="10" height="28" rx="5" fill="#e8f0fb"/>
  </g>
</svg>
`;
writeFileSync(join(root, 'public', 'images', 'auth-scene.svg'), scene, 'utf8');
writeFileSync(join(root, 'public', 'images', 'logo.svg'), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" role="img" aria-label="GameTrack">
  <defs><linearGradient id="l" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#4c45ff"/><stop offset="1" stop-color="#35c8ff"/></linearGradient></defs>
  <rect width="64" height="64" rx="16" fill="url(#l)"/>
  <rect x="12" y="20" width="40" height="26" rx="13" fill="#071f35"/>
  <circle cx="24" cy="33" r="4" fill="#35c8ff"/>
  <rect x="38" y="31" width="10" height="4" rx="2" fill="#e8f0fb"/>
  <rect x="41" y="28" width="4" height="10" rx="2" fill="#e8f0fb"/>
</svg>
`, 'utf8');

console.log('Assets generated in', coversDir);
