// damageController.js
const { Constants } = require('../config/constants');
const Utils = require('../services/utils');
const playerInGameController = require('./playerInGameController');
const { roomManager } = require('./roomManager');

class DamageController {
  constructor() {
    this.matchTTL = 60 * 15;           // 15 минут
    this.maxShotDistance = 50;

    // Параметры из Unity CharacterController
    this.playerCapsuleRadius = 0.5;     // CC Radius
    this.playerCapsuleHeight = 2.0;     // CC Height
    this.playerCapsuleCenter = { x: 0, y: 1, z: 0 }; // CC Center

    // Небольшой зазор под сетевые неточности (можно поднять до 0.1 при желании)
    this.hitPadding = 0.05;
  }

  // ---------------- vector utils ----------------
  sub(a, b) { return { x: a.x - b.x, y: a.y - b.y, z: a.z - b.z }; }
  add(a, b) { return { x: a.x + b.x, y: a.y + b.y, z: a.z + b.z }; }
  mul(a, k) { return { x: a.x * k, y: a.y * k, z: a.z * k }; }
  dot(a, b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
  length(a) { return Math.sqrt(this.dot(a, a)); }
  normalize(v) {
    const len = this.length(v);
    return (len > 1e-8) ? { x: v.x / len, y: v.y / len, z: v.z / len } : { x: 0, y: 0, z: 0 };
  }

  // Ближайшие точки между двумя отрезками P1Q1 и P2Q2.
  // Возвращает { s, t, c1, c2, dist2 }, где s,t в [0,1].
  closestPtSegmentSegment(P1, Q1, P2, Q2) {
    const EPS = 1e-8;
    const u = this.sub(Q1, P1);
    const v = this.sub(Q2, P2);
    const w = this.sub(P1, P2);
    const a = this.dot(u, u);
    const b = this.dot(u, v);
    const c = this.dot(v, v);
    const d = this.dot(u, w);
    const e = this.dot(v, w);
    let D = a * c - b * b;
    let sN, sD = D;
    let tN, tD = D;

    if (D < EPS) {
      sN = 0.0; sD = 1.0;
      tN = e;   tD = c;
    } else {
      sN = (b * e - c * d);
      tN = (a * e - b * d);
      if (sN < 0) { sN = 0; tN = e; tD = c; }
      else if (sN > sD) { sN = sD; tN = e + b; tD = c; }
    }

    if (tN < 0) {
      tN = 0;
      if (-d < 0) sN = 0;
      else if (-d > a) sN = sD;
      else { sN = -d; sD = a; }
    } else if (tN > tD) {
      tN = tD;
      if ((-d + b) < 0) sN = 0;
      else if ((-d + b) > a) sN = sD;
      else { sN = (-d + b); sD = a; }
    }

    const s = (Math.abs(sN) < EPS ? 0 : sN / sD);
    const t = (Math.abs(tN) < EPS ? 0 : tN / tD);

    const c1 = this.add(P1, this.mul(u, s));
    const c2 = this.add(P2, this.mul(v, t));
    const diff = this.sub(c1, c2);
    const dist2 = this.dot(diff, diff);
    return { s, t, c1, c2, dist2 };
  }

  // Проверка пересечения "луча-отрезка" с капсулой (ось капсулы — вертикальная по Y).
  // Возвращает { hit:bool, tRay:[0..1], point:{x,y,z} }
  checkHitCapsuleRay(origin, dirNorm, maxDist, capsuleBase, height, radius) {
    const P0 = origin;
    const P1 = this.add(origin, this.mul(dirNorm, maxDist));
    const A0 = capsuleBase;
    const A1 = { x: A0.x, y: A0.y + height, z: A0.z };

    const { s, c1, dist2 } = this.closestPtSegmentSegment(P0, P1, A0, A1);
    if (dist2 <= radius * radius) {
      return { hit: true, tRay: s, point: c1 };
    }
    return { hit: false, tRay: Infinity, point: null };
  }

  // ---------------- main ----------------
  async handleDealDamage(ws, data) {
    try {
      const required = [
        'attacker_id', 'room_id',
        'shot_origin_x', 'shot_origin_y', 'shot_origin_z',
        'shot_dir_x', 'shot_dir_y', 'shot_dir_z',
        'damage'
      ];
      for (const f of required) {
        if (data[f] === undefined || data[f] === null) {
          console.warn(`Missing damage data field: ${f}`);
          return Utils.sendError(ws, `Missing damage data field: ${f}`);
        }
      }

      const { attacker_id, room_id } = data;
      const baseDamage = Number(data.damage) || 0;
      const mode = data.pierce ? 'pierce' : (data.mode || 'single'); // 'single' | 'pierce'

      const shot_origin = { x: data.shot_origin_x, y: data.shot_origin_y, z: data.shot_origin_z };
      const dir = this.normalize({ x: data.shot_dir_x, y: data.shot_dir_y, z: data.shot_dir_z });
      if (dir.x === 0 && dir.y === 0 && dir.z === 0) {
        return Utils.sendError(ws, 'Invalid shot direction');
      }

      const room = await roomManager.getRoomInfo(room_id);
      if (!room) {
        console.warn(`Room not found: ${room_id}`);
        return Utils.sendError(ws, 'Room not found');
      }

      // Собираем цели: реальные игроки + боты
      const playerIds = Array.isArray(room.players) ? room.players : [];
      const botIds = Array.isArray(room.bots) ? room.bots.map(b => b && b.playerId).filter(Boolean) : [];
      const targetIds = [...new Set([...playerIds, ...botIds])].filter(id => id && id !== attacker_id);

      // (опционально) тихий лог на отладку
      // console.debug(`[Damage] room ${room_id}: targets=${targetIds.length} (players=${playerIds.length}, bots=${botIds.length})`);

      // Собираем хиты
      const hits = [];
      for (const playerId of targetIds) {
        const targetTransform = await playerInGameController.getPlayerTransform(playerId);
        if (!targetTransform || !targetTransform.position) continue;

        // Привязываем серверную капсулу к CC: база у пола
        const pos = targetTransform.position;
        const baseY = pos.y + this.playerCapsuleCenter.y - this.playerCapsuleHeight * 0.5;
        const capsuleBase = {
          x: pos.x + this.playerCapsuleCenter.x,
          y: baseY,
          z: pos.z + this.playerCapsuleCenter.z
        };
        const radius = this.playerCapsuleRadius + this.hitPadding;

        const result = this.checkHitCapsuleRay(
          shot_origin, dir, this.maxShotDistance,
          capsuleBase, this.playerCapsuleHeight, radius
        );

        if (result.hit) {
          hits.push({
            playerId,
            tHit: result.tRay,
            hitPoint: result.point
          });

          if (mode === 'single') break; // одиночный выстрел — берем первого попавшегося
        }
      }

      if (hits.length === 0) {
        // Сообщение об отсутствии попаданий (по желанию)
        ws.send(JSON.stringify({
          action: 'deal_damage_response',
          success: true,
          hit: false,
          attacker_id,
          room_id,
          shot_origin,
          shot_direction: dir
        }));
        return;
      }

      // Выбор целей
      hits.sort((a, b) => a.tRay - b.tRay);
      const targets = (mode === 'single') ? [hits[0]] : hits;

      // Пакетные обновления Redis
      const pipeline = global.redisClient.multi();
      const notifications = [];
      const deathEvents = [];

      let totalDamageToHp = 0;
      let killsToAdd = 0;

      for (const t of targets) {
        const playerId = t.playerId;
        const statsKey = `player_stats:${room_id}:${playerId}`;
        const targetStats = await global.redisClient.hGetAll(statsKey);
        if (!targetStats || targetStats.hp === undefined) {
          console.warn(`Stats not found for player: ${playerId}`);
          continue;
        }

        let hp = Number(targetStats.hp) || 0;
        let armor = Number(targetStats.armor) || 0;

        let damageToHp = 0;
        if (armor > 0) {
          if (baseDamage <= armor) {
            armor -= baseDamage;
          } else {
            const rest = baseDamage - armor;
            armor = 0;
            hp -= rest;
            damageToHp = rest;
          }
        } else {
          hp -= baseDamage;
          damageToHp = baseDamage;
        }

        totalDamageToHp += Math.max(0, damageToHp);

        if (hp < 0) hp = 0;
        if (armor < 0) armor = 0;

        pipeline.hSet(statsKey, 'hp', hp);
        pipeline.hSet(statsKey, 'armor', armor);

        // Смерть
        if (hp <= 0) {
          const deaths = (parseInt(targetStats.deaths || '0', 10) + 1);
          const respawnTime = Date.now() + 5000;

          pipeline.hSet(statsKey, 'deaths', deaths);
          pipeline.hSet(statsKey, 'respawn_time', respawnTime);
          pipeline.hSet(statsKey, 'hp', 0);
          pipeline.hSet(statsKey, 'armor', 0);

          if (attacker_id !== playerId) {
            killsToAdd += 1;
          }

          deathEvents.push({ player_id: playerId, room_id, killer_id: attacker_id, respawnTime });
        }

        // Для нотификаций
        notifications.push({
          action: 'player_damaged',
          attacker_id,
          target_id: playerId,
          damage: baseDamage,
          damage_to_hp: Math.max(0, damageToHp),
          new_hp: hp,
          new_armor: armor,
          hit_point: t.hitPoint,
          shot_origin,
          shot_direction: dir,
          timestamp: Date.now()
        });
      }

      // Статистика стрелка
      const attackerStatsKey = `player_stats:${room_id}:${attacker_id}`;
      if (totalDamageToHp >= 0) {
        pipeline.hIncrByFloat(attackerStatsKey, 'damage', totalDamageToHp);
      }
      if (killsToAdd > 0) {
        pipeline.hIncrBy(attackerStatsKey, 'kills', killsToAdd);
      }

      // Фиксация всех изменений
      await pipeline.exec();

      // Уведомляем игроков
      for (const n of notifications) {
        await roomManager.notifyRoomPlayers(room, n);

        // Ответ стрелку по каждой цели
        ws.send(JSON.stringify({
          action: 'deal_damage_response',
          success: true,
          hit: true,
          mode,
          attacker_id,
          target_id: n.target_id,
          damage: n.damage,
          damage_to_hp: n.damage_to_hp,
          new_hp: n.new_hp,
          new_armor: n.new_armor,
          room_id,
          shot_origin,
          shot_direction: dir,
          hit_point: n.hit_point
        }));
      }

      // Смерти — после фиксации Redis
      for (const d of deathEvents) {
        await playerInGameController.handlePlayerDeath(ws, {
          player_id: d.player_id,
          room_id: d.room_id,
          killer_id: d.killer_id
        });
      }

    } catch (error) {
      console.error('Damage handling error:', error);
      Utils.sendError(ws, 'Failed to deal damage');
    }
  }
}

module.exports = new DamageController();