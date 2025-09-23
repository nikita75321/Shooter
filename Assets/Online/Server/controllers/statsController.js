// controllers/statsController.js
const Utils = require('../services/utils');
const playerRedisService = require('../services/playerRedisService');

const LB_RATING = 'lb:rating';
const LB_KILLS  = 'lb:kills';
const PROFILE_KEY = (id) => `player:${id}:profile`;

/* ----------------------------- helpers ----------------------------- */

function num(v, def = 0) { const n = Number(v); return Number.isFinite(n) ? n : def; }

/**
 * Полная перестройка лидербордов из БД (батчами).
 * @param {Pool|Client} db - ваш клиент/пул БД (из initializeDatabase)
 * @param {Object} opts
 * @param {number} opts.batchSize - размер батча
 * @param {boolean} opts.clearExisting - удалять ли текущие ZSET’ы перед заливкой
 */

const R = () => global.redisClient;

async function rebuildLeaderboardsFromDB(db, { batchSize = 2000, clearExisting = true } = {}) {
  if (!global.redisClient) throw new Error('redisClient is not initialized');

  if (clearExisting) {
    await global.redisClient.del(LB_RATING, LB_KILLS);
  }

  let offset = 0;
  // ⚠️ Подправь имена колонок при необходимости (id, name, rating, overral_kill)
  const baseSQL = `
    SELECT id, player_id, player_name, rating, overral_kill
    FROM public.players
    ORDER BY id ASC
    LIMIT $1 OFFSET $2
  `;

  let total = 0;
  while (true) {
    const { rows } = await db.query(baseSQL, [batchSize, offset]);
    if (!rows.length) break;

    const pipe = global.redisClient.multi();

    for (const r of rows) {
      // используем player_id как ключ, а если его вдруг нет — подстрахуемся id
      const pid = String(r.player_id ?? r.id);
      const name = (r.player_name ?? '').toString();
      const rating = num(r.rating, 0);
      const kills  = num(r.overral_kill, 0);

      // Минимально прогреваем профиль, чтобы hydrateNames работал
      // (не трогаем TTL — пусть живёт без истечения; твой сервис потом сам обновит когда надо)
      // профиль — чтобы hydrateNames мог достать name
      pipe.hSet(PROFILE_KEY(pid), {
        player_name: name,
        rating: String(rating),
        overral_kill: String(kills),
      });

      // Лидерборды
      pipe.zAdd(LB_RATING, [{ score: rating, value: pid }]);
      pipe.zAdd(LB_KILLS,  [{ score: kills,  value: pid }]);
    }

    await pipe.exec();
    offset += rows.length;
    total += rows.length;
  }

  return total;
}

/**
 * Мягкая инициализация: если ZSET пусты — построить; если нет — ничего не делать.
 */
async function ensureLeaderboardsFromDB(db, opts = {}) {
  const [rc, kc] = await Promise.all([
    global.redisClient.zCard(LB_RATING),
    global.redisClient.zCard(LB_KILLS),
  ]);
  if (rc > 0 && kc > 0 && !opts.force) {
    return { skipped: true, rc, kc };
  }
  const total = await rebuildLeaderboardsFromDB(db, { clearExisting: true, ...(opts || {}) });
  const [rc2, kc2] = await Promise.all([
    global.redisClient.zCard(LB_RATING),
    global.redisClient.zCard(LB_KILLS),
  ]);
  return { skipped: false, total, rc: rc2, kc: kc2 };
}

// fallback на старые сервера Redis: ZREVRANGE WITHSCORES
async function zrevrangeWithScoresFallback(key, start, stop) {
  const raw = await R().sendCommand(['ZREVRANGE', key, String(start), String(stop), 'WITHSCORES']);
  const out = [];
  for (let i = 0; i < raw.length; i += 2) {
    const member = Buffer.isBuffer(raw[i]) ? raw[i].toString() : String(raw[i]);
    const score  = Number(Buffer.isBuffer(raw[i+1]) ? raw[i+1].toString() : raw[i+1]);
    out.push({ value: member, score });
  }
  return out;
}

async function getTopWithPlaces(zsetKey, limit = 30) {
  const start = 0, stop = Math.max(0, limit - 1);
  let rows;
  // Пробуем современный путь (node-redis v4 + Redis ≥ 6.2)
  if (typeof R().zRangeWithScores === 'function') {
    try {
      rows = await R().zRangeWithScores(zsetKey, start, stop, { REV: true });
    } catch (err) {
      // На старых Redis будет ERR syntax error → уходим на fallback
      if (String(err?.message || err).includes('ERR syntax error')) {
        rows = await zrevrangeWithScoresFallback(zsetKey, start, stop);
      } else {
        throw err;
      }
    }
  } else {
    // Если у клиента нет zRangeWithScores — сразу fallback
    rows = await zrevrangeWithScoresFallback(zsetKey, start, stop);
  }

  return rows.map((row, idx) => ({
    player_id: String(row.value),
    value: Number(row.score),
    place: idx + 1,
  }));
}

// Место и значение конкретного игрока
async function getMyRank(zsetKey, playerId) {
  const [rank, score] = await Promise.all([
    R().zRevRank(zsetKey, String(playerId)),
    R().zScore(zsetKey, String(playerId)),
  ]);
  if (rank === null || score === null) return null;
  return { place: rank + 1, value: Number(score) };
}

// Гидратация имён: HGET player:{id}:profile player_name
async function hydrateNames(entries) {
  if (!entries.length) return [];
  const pipe = R().multi();
  entries.forEach(e => pipe.hGet(PROFILE_KEY(e.player_id), 'player_name'));
  const res = await pipe.exec(); // node-redis v4 -> ['name1','name2',...]
  return entries.map((e, i) => ({
    ...e,
    name: (res?.[i] ?? '') || '',
  }));
}

// Нормализация под твой клиент
function normalizeLbEntry(e) {
  return {
    player_id: String(e.player_id),
    name: e.name || '',
    value: Number.isFinite(Number(e.value)) ? Math.trunc(Number(e.value)) : 0,
    place: String(e.place), // строкой
  };
}
function normalizeMyStats(e) {
  if (!e) return null;
  return {
    name: e.name || '',
    value: Number.isFinite(Number(e.value)) ? Math.trunc(Number(e.value)) : 0,
    place: String(e.place),
  };
}

/* ------------------------- update player stats ------------------------- */

async function handleUpdatePlayerStats(ws, data) {
  const required = ['player_id'];
  if (!Utils.isValidMessage(data, required)) {
    return Utils.sendError(ws, 'Missing required field: player_id');
  }

  try {
    const playerId = data.player_id;

    // 1) Текущий профиль из Redis
    let current = await playerRedisService.getPlayerFromRedis(playerId);
    if (!current) {
      current = {
        rating: 0,
        best_rating: 0,
        money: 0,
        donat_money: 0,
        overral_kill: 0,
        match_count: 0,
        win_count: 0,
        revive_count: 0,
        max_damage: 0,
        shoot_count: 0,
        love_hero: null,
        clan_points: 0,
        clan_id: null,
        clan_name: null,
        open_characters: {},
        hero_levels: [],
        hero_card: {},
        hero_lvl: [0, 0, 0],
        hero_match: Array(8).fill(0),
        player_name: data.player_name || null,
      };
    }

    // 2) Нормализация сложных полей
    const openCharacters = typeof current.open_characters === 'string'
      ? JSON.parse(current.open_characters) : (current.open_characters || {});
    const heroLevels = typeof current.hero_levels === 'string'
      ? JSON.parse(current.hero_levels) : (current.hero_levels || []);
    const heroCard = typeof current.hero_card === 'string'
      ? JSON.parse(current.hero_card) : (current.hero_card || {});
    const heroLvl = typeof current.hero_lvl === 'string'
      ? JSON.parse(current.hero_lvl) : (current.hero_lvl || [0,0,0]);
    const heroMatch = typeof current.hero_match === 'string'
      ? JSON.parse(current.hero_match) : (current.hero_match || Array(8).fill(0));

    // 3) Пересчёт метрик
    const newRating = Math.max(0, num(current.rating) + num(data.rating_change));
    const newMaxDamage = Math.max(num(current.max_damage), num(data.damage_dealt));
    const winInc = data.is_win ? 1 : 0;

    // 4) Апдейты
    const updates = {
      rating: newRating,
      best_rating: Math.max(num(current.best_rating), newRating),
      money: num(current.money) + num(data.money_change),
      donat_money: num(current.donat_money) + num(data.donat_money_change),
      overral_kill: num(current.overral_kill) + num(data.kills),
      match_count: num(current.match_count) + 1,
      win_count: num(current.win_count) + winInc,
      revive_count: num(current.revive_count) + num(data.revives),
      max_damage: newMaxDamage,
      shoot_count: num(current.shoot_count) + num(data.shots_fired),

      love_hero: data.favorite_hero ?? current.love_hero ?? null,
      player_name: data.player_name ?? current.player_name ?? null,

      open_characters: Object.assign(openCharacters, data.open_characters || {}),
      hero_levels: data.hero_levels || heroLevels,
      hero_card: data.hero_card || heroCard,
      hero_lvl: data.hero_lvl || heroLvl,
      hero_match: data.hero_match || heroMatch,
    };

    // 5) Сохраняем профиль через сервис
    await playerRedisService.savePlayerProfileToRedis(playerId, { ...current, ...updates });

    // 6) Обновляем лидерборды (node-redis v4)
    await R().multi()
      .zAdd(LB_RATING, [{ score: Number(updates.rating),       value: String(playerId) }])
      .zAdd(LB_KILLS,  [{ score: Number(updates.overral_kill), value: String(playerId) }])
      .exec();

    // 7) Ответ
    const out = { ...current, ...updates };
    ws.send(JSON.stringify({
      action: 'player_stats_updated_after_battle',
      stats: {
        rating: out.rating,
        best_rating: out.best_rating,
        money: out.money,
        donat_money: out.donat_money,
        overral_kill: out.overral_kill,
        match_count: out.match_count,
        win_count: out.win_count,
        revive_count: out.revive_count,
        max_damage: out.max_damage,
        shoot_count: out.shoot_count,
        love_hero: out.love_hero || null,
        clan_points: out.clan_points || 0,
        open_characters: out.open_characters,
        hero_levels: out.hero_levels,
        hero_card: out.hero_card,
        hero_lvl: out.hero_lvl,
        hero_match: out.hero_match,
        player_name: out.player_name || null,
      }
    }));

  } catch (err) {
    console.error('Update player stats error:', err);
    Utils.sendError(ws, err.message || 'Failed to update stats');
  }
}

/* --------------------------- leaderboards --------------------------- */

async function handleGetRatingLeaderboard(ws, data) {
  if (!Utils.isValidMessage(data, ['player_id'])) {
    return Utils.sendError(ws, 'Missing player_id');
  }
  try {
    const playerId = data.player_id;

    // Топ-30
    const topEntries = await getTopWithPlaces(LB_RATING, 30);
    const top_players = (await hydrateNames(topEntries)).map(normalizeLbEntry);

    // Позиция игрока отдельно
    const myRank = await getMyRank(LB_RATING, playerId);
    const my_stats = normalizeMyStats(
      myRank ? (await hydrateNames([{ player_id: playerId, ...myRank }]))[0] : null
    );

    ws.send(JSON.stringify({
      action: 'rating_leaderboard_response',
      top_players,
      my_stats
    }));
  } catch (err) {
    console.error('Error in handleGetRatingLeaderboard:', err);
    Utils.sendError(ws, 'Failed to get rating leaderboard');
  }
}

async function handleGetKillsLeaderboard(ws, data) {
  if (!Utils.isValidMessage(data, ['player_id'])) {
    return Utils.sendError(ws, 'Missing player_id');
  }
  try {
    const playerId = data.player_id;

    const topEntries = await getTopWithPlaces(LB_KILLS, 30);
    const top_players = (await hydrateNames(topEntries)).map(normalizeLbEntry);

    const myRank = await getMyRank(LB_KILLS, playerId);
    const my_stats = normalizeMyStats(
      myRank ? (await hydrateNames([{ player_id: playerId, ...myRank }]))[0] : null
    );

    ws.send(JSON.stringify({
      action: 'kills_leaderboard_response',
      top_players,
      my_stats
    }));
  } catch (err) {
    console.error('Error in handleGetKillsLeaderboard:', err);
    Utils.sendError(ws, 'Failed to get kills leaderboard');
  }
}

module.exports = {
  handleUpdatePlayerStats,
  handleGetRatingLeaderboard,
  handleGetKillsLeaderboard,
  ensureLeaderboardsFromDB,
  rebuildLeaderboardsFromDB
};