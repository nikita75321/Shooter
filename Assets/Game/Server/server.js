const fs = require('fs');
const https = require('https');
const WebSocket = require('ws');
const express = require('express');
const { Pool } = require('pg');
const os = require('os');
const path = require('path');
const multer = require('multer');

// SSL сертификаты
const sslOptions = {
    key: fs.readFileSync('/etc/letsencrypt/live/game.beatragdollsandbox.space/privkey.pem'),
    cert: fs.readFileSync('/etc/letsencrypt/live/game.beatragdollsandbox.space/fullchain.pem')
};

const app = express();
const server = https.createServer(sslOptions, app);
const port = process.env.PORT || 3000;

const util = require('util');
const clanController = require('../../Online/Server/controllers/clanController');
const logFile = fs.createWriteStream('server.log', { flags: 'a' });

// Переопределяем console.log
console.log = (msg, ...args) => {
    const timestamp = `[${getTimestamp()}]`;
    const formattedMsg = util.format(msg, ...args);

    const fullMessage = `${timestamp} ${formattedMsg}\n`;
    logFile.write(fullMessage);
    process.stdout.write(fullMessage);
};

// Настройки PostgreSQL
const pool = new Pool({
    user: process.env.DB_USER || 'admin_user',
    host: process.env.DB_HOST || 'localhost',
    database: process.env.DB_NAME || 'beatragdoll',
    password: process.env.DB_PASSWORD || 'RomaAndShukretSuperDevelopers',
    port: process.env.DB_PORT || 5432,
});

// WebSocket сервер
const wss = new WebSocket.Server({ server });

app.get('/cpu', (req, res) => {
    try {
        const load = os.loadavg()[0];
        const cpuCount = os.cpus().length;
        const cpuUsage = (load / cpuCount) * 100;

        res.json({
            cpuUsage: cpuUsage.toFixed(2),
            loadAverage: os.loadavg(),
            cpuCount: cpuCount
        });
    } catch (error) {
        console.error('CPU check error:', error);
        res.status(500).json({ error: 'Failed to get CPU usage' });
    }
});

// Конфигурация загрузки файлов
const uploadDir = '/var/www/html/uploads/';
const allowedTypes = ['image/png', 'image/jpeg'];
const maxFileSize = 5 * 1024 * 1024; // 5 МБ

// Создаем директорию, если её нет
if (!fs.existsSync(uploadDir)) {
  fs.mkdirSync(uploadDir, { recursive: true });
}

// Настройка multer
const storage = multer.diskStorage({
  destination: uploadDir,
  filename: (req, file, cb) => {
    const ext = path.extname(file.originalname);
    const filename = `level_${Date.now()}${ext}`;
    cb(null, filename);
  }
});

const fileFilter = (req, file, cb) => {
  if (allowedTypes.includes(file.mimetype)) {
    cb(null, true);
  } else {
    cb(new Error('Недопустимый тип файла'), false);
  }
};

const upload = multer({
  storage: storage,
  fileFilter: fileFilter,
  limits: { fileSize: maxFileSize }
}).single('screenshot');

// Хранилище данных в памяти
const rooms = new Map();
const playerData = new Map();

// const PING_INTERVAL = 2000; // 2 секунды
// const PING_TIMEOUT = 1000;  // 1 секунда на ответ

// const pingInterval = setInterval(() => {
//     wss.clients.forEach((ws) => {
//         // Если предыдущий ping не ответил - сразу закрываем
//         if (ws.__waitingForPong) {
//             console.log(`Terminating connection (no pong response): ${ws.playerId || 'unknown'}`);
//             ws.terminate();
//             return;
//         }

//         // Устанавливаем флаг ожидания pong
//         ws.__waitingForPong = true;
        
//         // Таймаут для ожидания pong
//         ws.__pongTimeout = setTimeout(() => {
//             if (ws.__waitingForPong && ws.readyState === WebSocket.OPEN) {
//                 console.log(`Force closing connection (ping timeout): ${ws.playerId || 'unknown'}`);
//                 ws.close(4000, 'Ping timeout');
//             }
//         }, PING_TIMEOUT);

//         // Отправляем ping
//         ws.ping();
//     });
// }, PING_INTERVAL);

server.listen(port, () => {
    console.log(`Secure WebSocket server running on port ${port}`);
});

// Вспомогательные функции
function getTimestamp() {
    return new Date().toISOString();
}

function uploadScreenshot(req, res, next) {
  upload(req, res, (err) => {
    if (err) {
      if (err.code === 'LIMIT_FILE_SIZE') {
        return res.status(413).json({ error: 'Файл слишком большой' });
      }
      if (err.message === 'Недопустимый тип файла') {
        return res.status(415).json({ error: err.message });
      }
      return res.status(500).json({ error: 'Ошибка загрузки файла' });
    }

    if (!req.file) {
      return res.status(400).json({ error: 'Файл не загружен' });
    }

    const fileUrl = `https://game.beatragdollsandbox.space/uploads/${req.file.filename}`;
    res.json({ url: fileUrl });
  });
}
async function handleUploadScreenshot(ws, data) {
    if (!isValidMessage(data, ['level_name', 'image_data'])) {
        return sendError(ws, 'Missing level_name or image_data');
    }

    const client = await pool.connect();
    try {
        // 1. Декодируем и сохраняем изображение
        const uploadDir = '/var/www/html/uploads/';
        if (!fs.existsSync(uploadDir)) {
            fs.mkdirSync(uploadDir, { recursive: true });
        }

        const fileName = `level_${Date.now()}.png`;
        const filePath = path.join(uploadDir, fileName);
        
        const base64Data = data.image_data.replace(/^data:image\/\w+;base64,/, '');
        const buffer = Buffer.from(base64Data, 'base64');
        fs.writeFileSync(filePath, buffer);

        // 2. Форматируем URL в нужный формат
        const imageUrl = {
            url: `https://game.beatragdollsandbox.space/uploads/${fileName}`
        };

        // 3. Обновляем запись в базе данных
        await client.query('BEGIN');
        
        const updateResult = await client.query(
            'UPDATE levels SET image_link = $1 WHERE level_name = $2',
            [imageUrl, data.level_name]
        );

        if (updateResult.rowCount === 0) {
            throw new Error('Level not found');
        }

        await client.query('COMMIT');

        // 4. Отправляем ответ клиенту
        ws.send(JSON.stringify({
            action: 'screenshot_upload_response',
            success: true,
            url: imageUrl.url // Отправляем полный URL для клиента
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Screenshot upload error:', error);
        sendError(ws, 'Failed to upload screenshot');
    } finally {
        client.release();
    }
}

function formatDateTime(date) {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    const seconds = String(d.getSeconds()).padStart(2, '0');

    return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}`;
}

function sendError(ws, message) {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
            action: 'error',
            message: message || 'Internal server error'
        }));
    }
}

function isValidMessage(data, requiredFields = []) {
    if (!data || typeof data !== 'object') return false;
    return requiredFields.every(field => data[field] !== undefined);
}

function handleBinaryMessage(ws, binaryData) {
    try {
        const buffer = Buffer.from(binaryData);
        let offset = 0;

        const actionLength = buffer.readUInt8(offset);
        offset += 1;

        const action = buffer.toString('utf8', offset, offset + actionLength);
        offset += actionLength;

        const jsonData = buffer.toString('utf8', offset);
        const data = JSON.parse(jsonData);
        data.action = action;

        return data;
    } catch (error) {
        console.error('Error processing binary message:', error);
        sendError(ws, 'Failed to process binary message');
        return null;
    }
}

// Обработчики сообщений
async function handleSaveLevelJson(ws, data) {
    if (!isValidMessage(data, ['level_name', 'level_json'])) {
        throw new Error('Missing required fields: level_name or level_json');
    }

    const client = await pool.connect();
    try {
        await client.query('BEGIN');
        
        const checkResult = await client.query(
            'SELECT id FROM levels WHERE level_name = $1',
            [data.level_name]
        );
        
        if (checkResult.rows.length === 0) {
            throw new Error('Level not found');
        }

        await client.query(
            'UPDATE levels SET level_json = $1 WHERE level_name = $2',
            [data.level_json, data.level_name]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'save_level_json_response',
            success: true,
            message: 'JSON данные уровня успешно обновлены'
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleSaveLevelJson:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

async function handleGetLevelJson(ws, data) {
    if (!isValidMessage(data, ['level_name'])) {
        throw new Error('Missing level_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT level_json FROM levels WHERE level_name = $1',
            [data.level_name]
        );

        if (result.rows.length > 0) {
            ws.send(JSON.stringify({
                action: 'get_level_json_response',
                success: true,
                level_json: result.rows[0].level_json
            }));
        } else {
            ws.send(JSON.stringify({
                action: 'get_level_json_response',
                success: false,
                error: 'Level not found'
            }));
        }
    } catch (error) {
        console.error('Error in handleGetLevelJson:', error);
        sendError(ws, 'Failed to get level JSON');
    } finally {
        client.release();
    }
}

async function handleShowRandom(ws) {
    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels ORDER BY RANDOM() LIMIT 20'
        );
        
        ws.send(JSON.stringify({
            action: 'show_random_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleShowRandom:', error);
        sendError(ws, 'Failed to get random levels');
    } finally {
        client.release();
    }
}

async function handleShowTopPlays(ws) {
    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels ORDER BY plays DESC LIMIT 20'
        );
        
        ws.send(JSON.stringify({
            action: 'show_top_plays_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleShowTopPlays:', error);
        sendError(ws, 'Failed to get top plays');
    } finally {
        client.release();
    }
}

async function handleShowTopRate(ws) {
    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels ORDER BY rate DESC LIMIT 20'
        );
        
        ws.send(JSON.stringify({
            action: 'show_top_rate_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleShowTopRate:', error);
        sendError(ws, 'Failed to get top rated levels');
    } finally {
        client.release();
    }
}

async function handleShowNewest(ws) {
    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels ORDER BY created_at DESC LIMIT 20'
        );
        
        ws.send(JSON.stringify({
            action: 'show_newest_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleShowNewest:', error);
        sendError(ws, 'Failed to get newest levels');
    } finally {
        client.release();
    }
}

async function handleCreateMap(ws, data) {
    const requiredFields = ['level_name', 'creator_name', 'level_json'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields');
    }

    const client = await pool.connect();
    try {
        await client.query('BEGIN');
        
        const result = await client.query(
            `INSERT INTO levels 
             (level_name, creator_name, level_json, rate, plays, image_link, vip)
             VALUES ($1, $2, $3, $4, $5, $6, $7)
             RETURNING id`,
            [
                data.level_name,
                data.creator_name,
                data.level_json,
                data.rate || 0,
                data.plays || 0,
                data.image_link || null,
                data.vip || false
            ]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'create_map_response',
            success: true,
            level_id: result.rows[0].id
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleCreateMap:', error);
        
        if (error.code === '23505') { // Unique violation
            sendError(ws, 'Level name already exists');
        } else {
            sendError(ws, 'Failed to create level');
        }
    } finally {
        client.release();
    }
}

async function handleSearchLevels(ws, data) {
    const client = await pool.connect();
    try {
        // Подготавливаем поисковые шаблоны
        const levelNamePattern = data.level_name ? `%${data.level_name.toLowerCase()}%` : null;
        const creatorNamePattern = data.creator_name ? `%${data.creator_name.toLowerCase()}%` : null;

        let query;
        let params = [];
        
        if (levelNamePattern && creatorNamePattern) {
            // Поиск и по названию уровня, и по имени создателя
            query = `
                SELECT * FROM levels 
                WHERE LOWER(level_name) LIKE $1 
                OR LOWER(creator_name) LIKE $2 
                ORDER BY 
                    CASE 
                        WHEN LOWER(level_name) LIKE $1 AND LOWER(creator_name) LIKE $2 THEN 1
                        WHEN LOWER(level_name) LIKE $1 THEN 2
                        ELSE 3
                    END,
                    plays DESC
                LIMIT 20
            `;
            params = [levelNamePattern, creatorNamePattern];
        } else if (levelNamePattern) {
            // Только по названию уровня
            query = `
                SELECT * FROM levels 
                WHERE LOWER(level_name) LIKE $1 
                ORDER BY plays DESC 
                LIMIT 20
            `;
            params = [levelNamePattern];
        } else if (creatorNamePattern) {
            // Только по имени создателя
            query = `
                SELECT * FROM levels 
                WHERE LOWER(creator_name) LIKE $1 
                ORDER BY plays DESC 
                LIMIT 20
            `;
            params = [creatorNamePattern];
        } else {
            // Если нет параметров поиска, возвращаем популярные уровни
            query = `
                SELECT * FROM levels 
                ORDER BY plays DESC 
                LIMIT 20
            `;
        }

        const result = await client.query(query, params);
        
        ws.send(JSON.stringify({
            action: 'search_levels_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleSearchLevels:', error);
        sendError(ws, 'Failed to search levels');
    } finally {
        client.release();
    }
}

async function handleFindByName(ws, data) {
    if (!isValidMessage(data, ['level_name'])) {
        throw new Error('Missing level_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels WHERE LOWER(level_name) = LOWER($1) LIMIT 20',
            [data.level_name]
        );
        
        ws.send(JSON.stringify({
            action: 'find_by_name_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleFindByName:', error);
        sendError(ws, 'Failed to find level by name');
    } finally {
        client.release();
    }
}

async function demoRequest(ws, data) {
    const client = await pool.connect();
    try {        
        ws.send(JSON.stringify({
            action: 'demo_response',
            demo_massage: true
        }));
    } catch (error) {
        console.error('Error in demoRequest:', error);
        sendError(ws, 'Failed to demoRequest');
    } finally {
        client.release();
    }
}

async function handleFindByAuthor(ws, data) {
    if (!isValidMessage(data, ['creator_name'])) {
        throw new Error('Missing creator_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT * FROM levels WHERE LOWER(creator_name) = LOWER($1) LIMIT 20',
            [data.creator_name]
        );
        
        ws.send(JSON.stringify({
            action: 'find_by_author_response',
            levels: result.rows
        }));
    } catch (error) {
        console.error('Error in handleFindByAuthor:', error);
        sendError(ws, 'Failed to find levels by author');
    } finally {
        client.release();
    }
}

async function handleIncrementPlays(ws, data) {
    if (!isValidMessage(data, ['level_name'])) {
        sendError(ws, 'Missing required field: level_name');
        return;
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'UPDATE levels SET plays = plays + 1 WHERE level_name = $1 RETURNING plays',
            [data.level_name]
        );

        if (result.rows.length === 0) {
            sendError(ws, 'Level not found');
            return;
        }

        ws.send(JSON.stringify({
            action: 'increment_plays_response',
            success: true,
            new_plays: result.rows[0].plays
        }));

    } catch (error) {
        console.error('Error in handleIncrementPlays:', error);
        sendError(ws, 'Failed to increment plays');
    } finally {
        client.release();
    }
}

async function handleHasRating(ws, data) {
    // Проверяем обязательные поля
    if (!isValidMessage(data, ['level_name', 'player_name'])) {
        sendError(ws, 'Missing required fields: level_name or player_name');
        return;
    }

    const client = await pool.connect();
    try {
        await client.query('BEGIN');
        
        // 1. Находим ID уровня
        const levelResult = await client.query(
            'SELECT id FROM levels WHERE level_name = $1',
            [data.level_name]
        );

        if (levelResult.rows.length === 0) {
            throw new Error('Level not found');
        }

        const levelId = levelResult.rows[0].id;

        // 2. Проверяем наличие оценки (используем player_name напрямую, так как players таблицы нет)
        const ratingResult = await client.query(
            'SELECT rating FROM level_ratings WHERE level_id = $1 AND player_id = $2 LIMIT 1',
            [levelId, data.player_name]
        );

        await client.query('COMMIT');
        
        const hasRating = ratingResult.rows.length > 0;
        const ratingValue = hasRating ? ratingResult.rows[0].rating : null;

        ws.send(JSON.stringify({
            action: 'check_rating_exists_response',
            exists: hasRating,
            rating: ratingValue,
            success: true
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleHasRating:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

async function handleRateLevel(ws, data) {
    if (!isValidMessage(data, ['level_name', 'player_id', 'rating'])) {
        throw new Error('Missing level_name, player_id or rating');
    }

    const client = await pool.connect();
    try {
        await client.query('BEGIN');
        
        const levelResult = await client.query(
            'SELECT id FROM levels WHERE level_name = $1',
            [data.level_name]
        );
        
        if (levelResult.rows.length === 0) {
            throw new Error('Level not found');
        }
        
        const level_id = levelResult.rows[0].id;
        const rating = parseInt(data.rating);

        const checkResult = await client.query(
            'SELECT 1 FROM level_ratings WHERE level_id = $1 AND player_id = $2',
            [level_id, data.player_id]
        );
        
        if (checkResult.rows.length > 0) {
            throw new Error('You have already rated this level');
        }

        await client.query(
            'INSERT INTO level_ratings (level_id, player_id, rating) VALUES ($1, $2, $3)',
            [level_id, data.player_id, rating]
        );

        await client.query(
            `UPDATE levels 
             SET 
                 total_rating = total_rating + $1,
                 rating_count = rating_count + 1,
                 rate = (total_rating + $1) / (rating_count + 1)
             WHERE id = $2`,
            [rating, level_id]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'rate_level_response',
            success: true
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleRateLevel:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

async function handleCreateName(ws, data) {
    if (!isValidMessage(data, ['creator_name'])) {
        throw new Error('Missing creator_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT id FROM levels WHERE creator_name = $1',
            [data.creator_name]
        );
        
        if (result.rows.length > 0) {
            ws.send(JSON.stringify({
                action: 'create_name_response',
                error: 'This name is already taken'
            }));
        } else {
            ws.send(JSON.stringify({
                action: 'create_name_response',
                available: true
            }));
        }
    } catch (error) {
        console.error('Error in handleCreateName:', error);
        sendError(ws, 'Failed to check name availability');
    } finally {
        client.release();
    }
}

async function handleCheckUserExists(ws, data) {
    if (!isValidMessage(data, ['creator_name'])) {
        return sendError(ws, 'Missing creator_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT EXISTS(SELECT 1 FROM levels WHERE creator_name = $1) as exists',
            [data.creator_name]
        );
        
        ws.send(JSON.stringify({
            action: 'check_user_exists_response',
            exists: result.rows[0].exists
        }));
    } catch (error) {
        console.error('Error in handleCheckUserExists:', error);
        sendError(ws, 'Failed to check user existence');
    } finally {
        client.release();
    }
}

async function handleCheckLevelExists(ws, data) {
    if (!isValidMessage(data, ['level_name'])) {
        return sendError(ws, 'Missing level_name');
    }

    const client = await pool.connect();
    try {
        const result = await client.query(
            'SELECT EXISTS(SELECT 1 FROM levels WHERE level_name = $1) as exists',
            [data.level_name]
        );
        
        ws.send(JSON.stringify({
            action: 'check_level_exists_response',
            exists: result.rows[0].exists
        }));
    } catch (error) {
        console.error('Error in handleCheckLevelExists:', error);
        sendError(ws, 'Failed to check level existence');
    } finally {
        client.release();
    }
}

async function handleUpdateImageLink(ws, data) {
    if (!isValidMessage(data, ['level_name', 'image_link'])) {
        throw new Error('Missing level_name or image_link');
    }

    const client = await pool.connect();
    try {
        await client.query('BEGIN');
        
        const checkResult = await client.query(
            'SELECT id FROM levels WHERE level_name = $1',
            [data.level_name]
        );
        
        if (checkResult.rows.length === 0) {
            throw new Error('Level not found');
        }

        await client.query(
            'UPDATE levels SET image_link = $1 WHERE level_name = $2',
            [data.image_link, data.level_name]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_image_link_response',
            success: true,
            message: 'Image link updated successfully'
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleUpdateImageLink:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

// Обработчик соединений
wss.on('connection', (ws) => {
    console.log('New client connected');
    ws.isAlive = true;
    // socket.setKeepAlive(true, 1000); // 1 секунда

    // const connectionTimeout = setTimeout(() => {
    //     if (ws.readyState === WebSocket.CONNECTING) {
    //         ws.close(4001, 'Connection timeout');
    //     }
    // }, 5000);

    ws.on('open', () => {
        clearTimeout(connectionTimeout);
    });

    ws.on('error', (error) => {
        console.error('WebSocket error:', error);
        clearTimeout(connectionTimeout);
        clearTimeout(ws.__pongTimeout);
        try { ws.close(4002, 'Connection error'); } catch {}
    });

    // ws.on('pong', () => {
    //     clearTimeout(ws.__pongTimeout);
    //     ws.isAlive = true;
    //     ws.__lastActivity = Date.now();
    // });
    
    ws.send(JSON.stringify({
        action: 'server_time_response',
        server_time: formatDateTime(new Date())
    }));
    
    // startPingPong(ws);
    // ws.on('error', (error) => {
    //     console.error('WebSocket error:', error);
    //     try {
    //         if (ws.readyState === WebSocket.OPEN) {
    //             ws.close(1011, 'Server error');
    //         }
    //     } catch (e) {
    //         console.error('Error closing connection:', e);
    //     }
    // });

    ws.on('message', async (message, isBinary) => {
        ws.__lastActivity = Date.now();
        try {
            // let data = isBinary ? handleBinaryMessage(ws, message) : JSON.parse(message);
            // if (!data?.action) throw new Error('Invalid message format');
            
            let data;
            if (isBinary) {
                data = handleBinaryMessage(ws, message);
            } else {
                data = JSON.parse(message);
            }

            if (!data) throw new Error('Invalid message format');
            
            if (data.action === 'ping') {
                return ws.send(JSON.stringify({
                    action: 'pong',
                    timestamp: Date.now()
                }));
            }

            switch (data.action) {
                case 'demo_request': await demoRequest(ws, data); break;

                case 'save_level_json': await handleSaveLevelJson(ws, data); break;
                case 'get_level_json': await handleGetLevelJson(ws, data); break;
                case 'show_random': await handleShowRandom(ws); break;
                case 'show_top_plays': await handleShowTopPlays(ws); break;
                case 'show_top_rate': await handleShowTopRate(ws); break;
                case 'show_newest': await handleShowNewest(ws); break;
                case 'create_map': await handleCreateMap(ws, data); break;
                case 'search_levels': await handleSearchLevels(ws, data); break;
                case 'find_by_name': await handleFindByName(ws, data); break;
                case 'find_by_author': await handleFindByAuthor(ws, data); break;
                case 'increment_plays': await handleIncrementPlays(ws, data); break;
                case 'has_rating': await handleHasRating(ws, data); break;
                case 'rate_level': await handleRateLevel(ws, data); break;
                case 'create_name': await handleCreateName(ws, data); break;
                case 'check_user_exists': await handleCheckUserExists(ws, data); break;
                case 'check_level_exists': await handleCheckLevelExists(ws, data); break;
                case 'update_image_link': await handleUpdateImageLink(ws, data); break;
                case 'upload_screenshot': await handleUploadScreenshot(ws, data); break;

                case 'get_time_until_month_end': await handleGetTimeUntilMonthEnd(ws); break;
                case 'get_game_modes_status': await handleGetGameModesStatus(ws); break;

                case 'register_player': await handleRegisterPlayer(ws, data); break;
                case 'player_connect': await handlePlayerConnect(ws, data); break;
                case 'update_player_rating': await handleUpdatePlayerRating(ws, data); break;
                case 'check_name': await handleCheckName(ws, data); break;
                case 'update_name': await handleUpdatePlayerName(ws, data); break;
                case 'update_rating': await handleUpdateRating(ws, data); break;
                case 'get_player_info': await handleGetPlayerInfo(ws, data); break;

                case 'claim_rewards': await handleClaimRewards(ws, data); break;
                case 'add_currency': await handleAddCurrency(ws, data); break;
                case 'spend_hero_cards': await handleSpendHeroCards(ws, data); break;

                case 'update_player_stats': await handleUpdatePlayerStats(ws, data); break;
                case 'update_hero_stats': await handleUpdateHeroStats(ws, data); break;
                case 'update_favorite_hero': await handleUpdateFavoriteHero(ws, data); break;
                case 'get_player_data': await handleGetPlayerData(ws, data); break;
                case 'get_hero_levels': await handleGetHeroLevels(ws, data); break;
                case 'update_hero_levels': await handleUpdateHeroLevels(ws, data); break;

                case 'create_clan': await clanController.CreateClan(ws, data); break;
                case 'join_clan': await handleJoinClan(ws, data); break;
                case 'leave_clan': await handleLeaveClan(ws, data); break;
                case 'get_all_clans': await handleGetAllClans(ws); break;
                case 'search_clans': await handleSearchClans(ws, data); break;
                case 'get_clan_info': await handleGetClanInfo(ws, data); break;
                case 'get_clan_top_with_current': await handleGetClanInfoWithTop(ws, data); break;

                case "get_rating_leaderboard": handleGetRatingLeaderboard(ws, data); break;
                case "get_kills_leaderboard": handleGetKillsLeaderboard(ws, data); break;
                
                default:
                    console.warn(`Unknown action: ${data.action}`);
                    sendError(ws, `Unknown action: ${data.action}`);
            }
        } catch (error) {
            console.error('Error processing message:', error);
            sendError(ws, error.message);
        }
    });

    ws.on('close', (code, reason) => {
        clearTimeout(connectionTimeout);
        clearTimeout(ws.__pongTimeout);
        console.log(`Client disconnected. Code: ${code}, Reason: ${reason}`);
        handlePlayerDisconnect(ws).catch(console.error);
    });
    
    // ws.on('close', () => {
    //     clearTimeout(ws.__pongTimeout);
    // });
});

function startPingPong(ws) {
    const pingInterval = setInterval(() => {
        if (ws.readyState !== WebSocket.OPEN) {
            return clearInterval(pingInterval);
        }

        // Если предыдущий ping не ответил
        if (!ws.isAlive) {
            console.log('No pong response, terminating');
            return ws.terminate();
        }

        ws.isAlive = false;
        ws.__pongTimeout = setTimeout(() => {
            if (ws.readyState === WebSocket.OPEN && !ws.isAlive) {
                console.log('Pong timeout, closing');
                ws.close(4003, 'Pong timeout');
            }
        }, 1000); // 1 секунда на ответ

        ws.ping();
    }, 2000); // Ping каждые 2 секунды

    // Очистка при закрытии соединения
    ws.on('close', () => clearInterval(pingInterval));
}

//Регистрация нового игрока
async function handleRegisterPlayer(ws, data) {
    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // Регистрируем нового игрока
        const insertResult = await client.query(
            `INSERT INTO players 
             (player_name, platform, open_characters, love_hero) 
             VALUES ($1, $2, $3, $4)
             RETURNING id`,
            [
                data.player_name,
                data.platform || 'неизвестно',
                data.open_characters || '{}',
                data.love_hero || 'нет'
            ]
        );
        
        // Генерируем player_id в формате "id-рандомное число"
        const playerId = `${insertResult.rows[0].id}-${Math.floor(10000 + Math.random() * 90000)}`;
        
        // Обновляем запись с player_id
        await client.query(
            'UPDATE players SET player_id = $1 WHERE id = $2',
            [playerId, insertResult.rows[0].id]
        );
        
        // Получаем полные данные игрока
        const result = await client.query(
            'SELECT id, player_name, player_id FROM players WHERE id = $1',
            [insertResult.rows[0].id]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'register_player_response',
            success: true,
            id: result.rows[0].id,
            player_name: result.rows[0].player_name,
            player_id: result.rows[0].player_id
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Register player error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

async function handlePlayerDisconnect(ws) {
    console.log(`[DISCONNECT] Handling disconnect for player`);
}

pool.query('SELECT NOW()')
    .then(() => console.log('Database connected successfully'))
    .catch(err => {
        console.error('Database connection error:', err);
        process.exit(1);
    });

console.log('WebSocket server started');

app.post('/upload-screenshot', uploadScreenshot);




//#region  Shooter

const shooterPool = new Pool({
    user: process.env.DB_USER || 'admin_user',
    host: process.env.DB_HOST || 'localhost',
    database: 'shooter', // Указываем именно базу shooter
    password: process.env.DB_PASSWORD || 'RomaAndShukretSuperDevelopers',
    port: process.env.DB_PORT || 5432,
});

shooterPool.query('SELECT NOW()')
    .then(() => console.log('Shooter database connected successfully'))
    .catch(err => {
        console.error('Shooter database connection error:', err);
        process.exit(1);
    });

//#region Gamemode
async function handleGetGameModesStatus(ws) {
    const now = Date.now();
    
    // Режим 2: 30 сек доступен, 45 сек закрыт (вместо 30 мин/60 мин)
    const mode2Cycle = 75 * 1000; // 75 секунд цикл (30+45)
    const mode2CyclePos = now % mode2Cycle;
    const mode2Available = mode2CyclePos < (30 * 1000);
    const mode2TimeLeft = mode2Available 
        ? (30 * 1000 - mode2CyclePos) / 1000 
        : (75 * 1000 - mode2CyclePos) / 1000;

    // Режим 3: 45 сек доступен, 60 сек закрыт (вместо 60 мин/120 мин)
    const mode3Cycle = 105 * 1000; // 105 секунд цикл (45+60)
    const mode3CyclePos = now % mode3Cycle;
    const mode3Available = mode3CyclePos < (45 * 1000);
    const mode3TimeLeft = mode3Available 
        ? (45 * 1000 - mode3CyclePos) / 1000 
        : (105 * 1000 - mode3CyclePos) / 1000;

    const response = {
        action: 'game_modes_status_response',
        modes: {
            mode1: { available: true, timeLeft: 0 }, // Первый режим всегда доступен
            mode2: { available: mode2Available, timeLeft: mode2TimeLeft },
            mode3: { available: mode3Available, timeLeft: mode3TimeLeft }
        },
        serverTime: now
    };
    
    ws.send(JSON.stringify(response));
}
//#endregion





//#region Temp
async function handleGetTimeUntilMonthEnd(ws) {
    try {
        const now = new Date();
        const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59);
        const diffMs = endOfMonth - now;
        
        // Конвертируем миллисекунды в дни, часы, минуты
        const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        const hours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
        
        ws.send(JSON.stringify({
            action: 'time_until_month_end_response',
            days: days,
            hours: hours,
            minutes: minutes
        }));
    } catch (error) {
        console.error('Error in handleGetTimeUntilMonthEnd:', error);
        sendError(ws, 'Failed to calculate time until month end');
    }
}
//#endregion
// Создание/обновление игрока
async function handlePlayerConnect(ws, data) {
    if (!isValidMessage(data, ['player_id', 'player_name'])) {
        throw new Error('Missing required fields: player_id or player_name');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // Проверяем существование игрока
        const checkResult = await client.query(
            'SELECT 1 FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (checkResult.rows.length > 0) {
            // Обновляем последнее время онлайн
            await client.query(
                'UPDATE players SET last_online = NOW(), player_name = $1 WHERE player_id = $2',
                [data.player_name, data.player_id]
            );
        } else {
            // Создаем нового игрока
            await client.query(
                'INSERT INTO players (player_id, player_name) VALUES ($1, $2)',
                [data.player_id, data.player_name]
            );
        }
        
        await client.query('COMMIT');
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handlePlayerConnect:', error);
    } finally {
        client.release();
    }
}
// Получение информации об игроке
async function handleGetPlayerInfo(ws, data) {
    if (!isValidMessage(data, ['player_id'])) {
        throw new Error('Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            'SELECT player_id, player_name, rating FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (result.rows.length > 0) {
            ws.send(JSON.stringify({
                action: 'get_player_info_response',
                player: result.rows[0]
            }));
        } else {
            sendError(ws, 'Player not found');
        }
    } catch (error) {
        console.error('Error in handleGetPlayerInfo:', error);
        sendError(ws, 'Failed to get player info');
    } finally {
        client.release();
    }
}
// Обновление рейтинга игрока
async function handleUpdatePlayerRating(ws, data) {
    if (!isValidMessage(data, ['player_id', 'rating_change'])) {
        throw new Error('Missing player_id or rating_change');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        await client.query(
            'UPDATE players SET rating = rating + $1 WHERE player_id = $2',
            [data.rating_change, data.player_id]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_rating_response',
            success: true
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleUpdatePlayerRating:', error);
        sendError(ws, 'Failed to update player rating');
    } finally {
        client.release();
    }
}
// Проверка доступности имени
async function handleCheckName(ws, data) {
    if (!isValidMessage(data, ['player_name'])) {
        throw new Error('Missing player_name');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            'SELECT 1 FROM players WHERE player_name = $1',
            [data.player_name]
        );
        
        ws.send(JSON.stringify({
            action: 'check_name_response',
            available: result.rows.length === 0,
            requested_name: data.player_name
        }));
    } catch (error) {
        console.error('Error in handleCheckName:', error);
        sendError(ws, 'Failed to check name availability');
    } finally {
        client.release();
    }
}

// Обновление имени игрока
async function handleUpdatePlayerName(ws, data) {
    if (!isValidMessage(data, ['player_id', 'new_name'])) {
        return sendError(ws, 'Missing player_id or new_name');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // 1. Получаем внутренний ID игрока по его player_id (строке)
        const playerRes = await client.query(
            'SELECT id FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (playerRes.rows.length === 0) {
            throw new Error('Player not found');
        }
        const playerId = playerRes.rows[0].id;

        // 2. Проверка на существование имени (исключая текущего игрока)
        const nameCheck = await client.query(
            'SELECT 1 FROM players WHERE player_name = $1 AND player_id != $2',
            [data.new_name, data.player_id]
        );
        
        if (nameCheck.rows.length > 0) {
            throw new Error('Player name already taken');
        }

        // 3. Обновление имени в таблице players (по строковому player_id)
        await client.query(
            'UPDATE players SET player_name = $1 WHERE player_id = $2',
            [data.new_name, data.player_id]
        );
        
        // 4. Обновление имени в clan_members (по внутреннему числовому ID)
        await client.query(
            'UPDATE clan_members SET player_name = $1 WHERE player_id = $2',
            [data.new_name, playerId]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_name_response',
            success: true,
            new_name: data.new_name
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error updating player name:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обновление рейтинга
async function handleUpdateRating(ws, data) {
    if (!isValidMessage(data, ['player_name', 'rating_change'])) {
        throw new Error('Missing player_name or rating_change');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        await client.query(
            'UPDATE players SET rating = rating + $1 WHERE player_name = $2',
            [data.rating_change, data.player_name]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_rating_response',
            success: true
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleUpdateRating:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
//#region Clans
// Создание клана
async function handleCreateClan(ws, data) {
    const requiredFields = ['clan_name', 'leader_id', 'leader_name'];
    if (!isValidMessage(data, requiredFields)) {
        sendError(ws, 'Missing required fields');
        return;
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // 1. Проверка существования клана
        const clanCheck = await client.query(
            'SELECT 1 FROM clans WHERE LOWER(clan_name) = LOWER($1)',
            [data.clan_name]
        );
        if (clanCheck.rows.length > 0) {
            throw new Error('Clan name already exists');
        }

        // 2. Получаем числовой ID игрока по его player_id (например "27-89804")
        const playerQuery = await client.query(
            'SELECT id FROM players WHERE player_id = $1',
            [data.leader_id]
        );
        
        if (playerQuery.rows.length === 0) {
            throw new Error('Leader not found');
        }
        
        const leaderId = playerQuery.rows[0].id;

        // 3. Проверяем, что игрок не состоит в другом клане
        const clanMemberCheck = await client.query(
            'SELECT 1 FROM clan_members WHERE player_id = $1',
            [leaderId]
        );
        
        if (clanMemberCheck.rows.length > 0) {
            throw new Error('Leader already in another clan');
        }

        // 4. Создание клана
        const clanResult = await client.query(
            `INSERT INTO clans 
             (clan_name, leader_id, leader_name, need_rating, is_open)
             VALUES ($1, $2, $3, $4, $5)
             RETURNING clan_id, clan_name`,
            [
                data.clan_name,
                leaderId, // Используем числовой ID из таблицы players
                data.leader_name,
                data.need_rating || 0,
                data.is_open !== false
            ]
        );
        
        const clanId = clanResult.rows[0].clan_id;
        const clanName = clanResult.rows[0].clan_name;

        // 5. Добавление лидера в clan_members
        await client.query(
            `INSERT INTO clan_members 
             (clan_id, player_name, player_id, is_leader)
             VALUES ($1, $2, $3, true)`,
            [clanId, data.leader_name, leaderId]
        );

        // 6. Обновление информации игрока
        await client.query(
            'UPDATE players SET clan_name = $1 WHERE id = $2',
            [clanName, leaderId]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'create_clan_response',
            success: true,
            clan_id: clanId,
            clan_name: clanName,
            leader_id: data.leader_id, // Возвращаем оригинальный player_id
            leader_name: data.leader_name
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Clan creation error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

// Вступление в клан
async function handleJoinClan(ws, data) {
    const requiredFields = ['clan_id', 'player_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, `Missing required fields: ${requiredFields.join(', ')}`);
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Получаем полные данные клана
        const clanCheck = await client.query(
            `SELECT 
                clan_id,
                clan_name,
                is_open,
                need_rating,
                max_players,
                player_count 
             FROM clans 
             WHERE clan_id = $1
             FOR UPDATE`,
            [data.clan_id]
        );
        
        if (clanCheck.rows.length === 0) {
            throw new Error('Clan not found');
        }
        const clan = clanCheck.rows[0];

        // 2. Получаем данные игрока, включая внутренний id
        const playerCheck = await client.query(
            `SELECT 
                id,
                player_name,
                rating,
                clan_name 
             FROM players 
             WHERE player_id = $1
             FOR UPDATE`,
            [data.player_id]
        );

        if (playerCheck.rows.length === 0) {
            throw new Error('Player not found');
        }
        const player = playerCheck.rows[0];

        // 3. Проверки условий вступления
        if (player.clan_name) throw new Error('Player already in another clan');
        if (!clan.is_open) throw new Error('Clan is closed for new members');
        if (player.rating < clan.need_rating) {
            throw new Error(`Player rating too low (need ${clan.need_rating})`);
        }
        if (clan.player_count >= clan.max_players) {
            throw new Error('Clan is full');
        }

        // 4. Добавляем игрока в клан (используем внутренний id из таблицы players)
        await client.query(
            `INSERT INTO clan_members 
             (clan_id, player_name, player_id, is_leader) 
             VALUES ($1, $2, $3, false)`,
            [data.clan_id, player.player_name, player.id] // Используем player.id вместо data.player_id
        );

        // // 5. Обновляем счетчик игроков в клане
        // await client.query(
        //     'UPDATE clans SET player_count = player_count + 1 WHERE clan_id = $1',
        //     [data.clan_id]
        // );

        // 6. Обновляем клан игрока
        await client.query(
            'UPDATE players SET clan_name = $1 WHERE id = $2',
            [clan.clan_name, player.id]
        );

        await client.query('COMMIT');

        // 7. Формируем ответ
        const response = {
            action: 'join_clan_response',
            success: true,
            clan_id: clan.clan_id,
            clan_name: clan.clan_name,
            player_count: clan.player_count,
            player_id: player.id, // Возвращаем внутренний id игрока
            player_name: player.player_name
        };

        ws.send(JSON.stringify(response));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Join clan error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обработчик для обычного выхода из клана
async function handleLeaveClan(ws, data) {
    const requiredFields = ['player_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required field: player_id');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Получаем внутренний ID игрока
        const playerRes = await client.query(
            'SELECT id FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (playerRes.rows.length === 0) {
            throw new Error('Player not found');
        }
        const playerId = playerRes.rows[0].id;

        // 2. Получаем данные клана
        const clanRes = await client.query(
            `SELECT cm.clan_id, cm.is_leader, c.clan_name 
             FROM clan_members cm
             JOIN clans c ON cm.clan_id = c.clan_id
             WHERE cm.player_id = $1 FOR UPDATE`,
            [playerId]
        );
        
        if (clanRes.rows.length === 0) {
            throw new Error('Player not in any clan');
        }
        const clanId = clanRes.rows[0].clan_id;
        const isLeader = clanRes.rows[0].is_leader;
        const clanName = clanRes.rows[0].clan_name;

        // 3. Удаляем игрока из клана (триггер автоматически обновит clan_points)
        await client.query(
            'DELETE FROM clan_members WHERE player_id = $1',
            [playerId]
        );

        // 4. Обновляем запись игрока
        await client.query(
            'UPDATE players SET clan_name = NULL, clan_points = 0 WHERE id = $1',
            [playerId]
        );

        // 5. Обработка лидера
        let clanDeleted = false;
        if (isLeader) {
            const newLeaderRes = await client.query(
                `SELECT cm.player_id, p.player_name 
                 FROM clan_members cm
                 JOIN players p ON cm.player_id = p.id
                 WHERE cm.clan_id = $1 
                 ORDER BY p.rating DESC 
                 LIMIT 1 FOR UPDATE`,
                [clanId]
            );

            if (newLeaderRes.rows.length > 0) {
                const newLeader = newLeaderRes.rows[0];
                await client.query(
                    'UPDATE clans SET leader_id = $1, leader_name = $2 WHERE clan_id = $3',
                    [newLeader.player_id, newLeader.player_name, clanId]
                );
                await client.query(
                    'UPDATE clan_members SET is_leader = true WHERE player_id = $1 AND clan_id = $2',
                    [newLeader.player_id, clanId]
                );
            } else {
                await client.query('DELETE FROM clans WHERE clan_id = $1', [clanId]);
                clanDeleted = true;
            }
        }

        await client.query('COMMIT');

        ws.send(JSON.stringify({
            action: 'leave_clan_response',
            success: true,
            player_id: data.player_id,
            was_leader: isLeader,
            clan_deleted: clanDeleted,
            clan_name: clanName
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Leave clan error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обработчик для передачи лидерства
async function handleTransferLeadership(ws, data) {
    const requiredFields = ['current_leader_id', 'new_leader_id', 'clan_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Проверяем, что текущий пользователь действительно лидер
        const leaderCheck = await client.query(
            'SELECT 1 FROM clans WHERE leader_id = $1 AND clan_id = $2',
            [data.current_leader_id, data.clan_id]
        );
        
        if (leaderCheck.rows.length === 0) {
            throw new Error('Only clan leader can transfer leadership');
        }

        // 2. Проверяем, что новый лидер состоит в клане
        const memberCheck = await client.query(
            'SELECT 1 FROM clan_members WHERE player_id = $1 AND clan_id = $2',
            [data.new_leader_id, data.clan_id]
        );
        
        if (memberCheck.rows.length === 0) {
            throw new Error('New leader must be a clan member');
        }

        // 3. Получаем имя нового лидера
        const nameRes = await client.query(
            'SELECT player_name FROM players WHERE player_id = $1',
            [data.new_leader_id]
        );
        const newLeaderName = nameRes.rows[0].player_name;

        // 4. Обновляем лидера в клане
        await client.query(`
            UPDATE clans SET 
                leader_id = $1,
                leader_name = $2 
            WHERE clan_id = $3`,
            [data.new_leader_id, newLeaderName, data.clan_id]);
        
        // 5. Обновляем флаги лидерства у участников
        await client.query(`
            UPDATE clan_members SET 
                is_leader = (player_id = $1)
            WHERE clan_id = $2`,
            [data.new_leader_id, data.clan_id]);

        await client.query('COMMIT');

        ws.send(JSON.stringify({
            action: 'transfer_leadership_response',
            success: true,
            clan_id: data.clan_id,
            new_leader_id: data.new_leader_id,
            new_leader_name: newLeaderName
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Transfer leadership error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обработчик получения списка кланов
async function handleGetAllClans(ws) {
    const client = await shooterPool.connect();
    try {
        // Получаем список кланов с уникальными местами
        const result = await client.query(
            `WITH ranked_clans AS (
                SELECT 
                    c.clan_id,
                    c.clan_name,
                    c.leader_name,
                    c.need_rating,
                    c.is_open,
                    c.clan_points,
                    c.clan_level,
                    c.max_players,
                    COUNT(cm.player_name) as player_count,
                    SUM(p.clan_points) as members_points_sum,
                    ROW_NUMBER() OVER (ORDER BY c.clan_points DESC, c.clan_id) as place
                FROM clans c
                LEFT JOIN clan_members cm ON c.clan_id = cm.clan_id
                LEFT JOIN players p ON cm.player_id = p.id
                GROUP BY c.clan_id
            )
            SELECT * FROM ranked_clans
            ORDER BY place`
        );
        
        // Формируем ответ
        const clans = result.rows.map(clan => ({
            clan_id: clan.clan_id,
            clan_name: clan.clan_name,
            leader_name: clan.leader_name,
            need_rating: clan.need_rating,
            is_open: clan.is_open,
            clan_points: clan.clan_points,
            clan_level: clan.clan_level,
            player_count: clan.player_count,
            max_players: clan.max_players,
            points_valid: clan.clan_points === clan.members_points_sum,
            place: clan.place  // Уникальное место для каждого клана
        }));
        
        ws.send(JSON.stringify({
            action: 'get_all_clans_response',
            clans: clans,
            server_time: new Date().toISOString()
        }));
        
    } catch (error) {
        console.error('Error getting clans:', error);
        sendError(ws, 'Failed to get clan list: ' + error.message);
    } finally {
        client.release();
    }
}
// Получение топ 10 кланов и клана в котом мы находимся
async function handleGetClanInfoWithTop(ws, data) {
    if (!data.player_id && !data.clan_id) {
        return sendError(ws, 'Provide either player_id or clan_id');
    }

    const client = await shooterPool.connect();
    try {
        const query = `
            WITH ranked_clans AS (
                SELECT 
                    c.clan_id, c.clan_name, c.leader_name, c.leader_id,
                    c.need_rating, c.is_open, c.clan_points, c.clan_level,
                    c.max_players, c.player_count,
                    ROW_NUMBER() OVER (ORDER BY c.clan_points DESC, c.clan_id) as place
                FROM clans c
            ),
            current_clan AS (
                SELECT clan_id 
                FROM clan_members 
                WHERE player_id = $1 OR clan_id = $2
                LIMIT 1
            )
            SELECT 
                rc.*,
                CASE WHEN EXISTS (SELECT 1 FROM current_clan WHERE clan_id = rc.clan_id) 
                     THEN true ELSE false END as is_current_clan
            FROM ranked_clans rc
            WHERE 
                rc.place <= 10 OR
                EXISTS (SELECT 1 FROM current_clan WHERE clan_id = rc.clan_id)
            ORDER BY rc.place`;

        const result = await client.query(query, [
            data.player_id || null, 
            data.clan_id || null
        ]);

        const allClans = result.rows;
        const currentClan = allClans.find(c => c.is_current_clan) || null;
        
        // Формируем топ-10, гарантированно включая текущий клан если он в топе
        let topClans = allClans.filter(c => c.place <= 10);
        
        // Если текущий клан не в топе, заменяем последний в топе на текущий клан
        if (currentClan && currentClan.place > 10 && topClans.length >= 10) {
            topClans[9] = currentClan; // Заменяем 10-й элемент
        }

        const response = {
            action: 'clan_info_with_top_response',
            success: true,
            top_clans: topClans,
            current_clan: currentClan
        };

        ws.send(JSON.stringify(response));

    } catch (error) {
        console.error('Error in handleGetClanInfoWithTop:', error);
        ws.send(JSON.stringify({
            action: 'clan_info_with_top_response',
            success: false,
            error: error.message || 'Unknown error'
        }));
    } finally {
        client.release();
    }
}

async function handleGetClanInfo(ws, data) {
    if (!data.clan_id && !data.clan_name) {
        return sendError(ws, 'Provide either clan_id or clan_name');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Получаем расширенную информацию о клане (включая clan_level)
        const clanQuery = data.clan_id 
            ? `SELECT 
                  c.clan_id, c.clan_name, c.leader_id, c.leader_name,
                  c.need_rating, c.is_open, c.player_count, 
                  c.max_players, c.clan_points, c.clan_level,
                  SUM(p.clan_points) as calculated_points
               FROM clans c
               LEFT JOIN clan_members cm ON c.clan_id = cm.clan_id
               LEFT JOIN players p ON cm.player_id = p.id
               WHERE c.clan_id = $1
               GROUP BY c.clan_id`
            : `SELECT 
                  c.clan_id, c.clan_name, c.leader_id, c.leader_name,
                  c.need_rating, c.is_open, c.player_count, 
                  c.max_players, c.clan_points, c.clan_level,
                  SUM(p.clan_points) as calculated_points
               FROM clans c
               LEFT JOIN clan_members cm ON c.clan_id = cm.clan_id
               LEFT JOIN players p ON cm.player_id = p.id
               WHERE c.clan_name = $1
               GROUP BY c.clan_id`;
        
        const clanParam = data.clan_id || data.clan_name;
        const clanResult = await client.query(clanQuery, [clanParam]);

        if (clanResult.rows.length === 0) {
            throw new Error('Clan not found');
        }
        const clan = clanResult.rows[0];

        // Проверка синхронизации очков
        const pointsValid = clan.clan_points === clan.calculated_points;

        // 2. Получаем рейтинг лидера
        const leaderRatingRes = await client.query(
            `SELECT rating, best_rating 
             FROM players 
             WHERE id = $1`,
            [clan.leader_id]
        );
        const leaderRating = leaderRatingRes.rows[0]?.rating || 0;
        const leaderBestRating = leaderRatingRes.rows[0]?.best_rating || 0;

        // 3. Получаем участников клана с их статистикой
        const membersRes = await client.query(
            `SELECT 
                cm.player_id,
                cm.player_name,
                cm.is_leader,
                p.rating,
                p.best_rating,
                p.clan_points,
                p.overral_kill,
                p.match_count,
                p.win_count
             FROM clan_members cm
             JOIN players p ON cm.player_id = p.id
             WHERE cm.clan_id = $1
             ORDER BY cm.is_leader DESC, p.rating DESC`,
            [clan.clan_id]
        );

        // 4. Рассчитываем прогресс до следующего уровня
        const nextLevelThreshold = getNextLevelThreshold(clan.clan_level);
        const levelProgress = nextLevelThreshold 
            ? Math.min(100, Math.round((clan.clan_points / nextLevelThreshold) * 100))
            : 100;

        // 5. Получаем место клана в общем рейтинге
        const placeRes = await client.query(
            `SELECT place FROM (
                SELECT 
                    clan_id, 
                    RANK() OVER (ORDER BY clan_points DESC) as place
                FROM clans
            ) ranked_clans
            WHERE clan_id = $1`,
            [clan.clan_id]
        );
        const place = placeRes.rows[0]?.place || 0;

        await client.query('COMMIT');

        // 6. Формируем расширенный ответ с информацией об уровне
        const response = {
            action: 'get_clan_info_response',
            success: true,
            clan: {
                id: clan.clan_id,
                name: clan.clan_name,
                leader: {
                    id: clan.leader_id,
                    name: clan.leader_name,
                    rating: leaderRating,
                    best_rating: leaderBestRating
                },
                stats: {
                    place: place,
                    points: clan.clan_points,
                    player_count: clan.player_count,
                    max_players: clan.max_players,
                    current_level: clan.clan_level,
                    next_level_progress: levelProgress,
                    need_rating: clan.need_rating,
                    is_open: clan.is_open,
                    points_valid: pointsValid,
                    points_breakdown: {
                        current: clan.clan_points,
                        calculated: clan.calculated_points,
                        difference: clan.clan_points - (clan.calculated_points || 0)
                    }
                }
            },
            members: membersRes.rows.map(member => ({
                id: member.player_id,
                name: member.player_name,
                is_leader: member.is_leader,
                stats: {
                    rating: member.rating,
                    best_rating: member.best_rating,
                    clan_points: member.clan_points,
                    kills: member.overral_kill,
                    matches: member.match_count,
                    wins: member.win_count,
                    contribution_percent: clan.clan_points > 0 
                        ? Math.round((member.clan_points / clan.clan_points) * 100)
                        : 0
                }
            }))
        };

        ws.send(JSON.stringify(response));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Get clan info error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}

// Вспомогательная функция для определения порога следующего уровня
function getNextLevelThreshold(currentLevel) {
    const thresholds = {
        0: 150,
        1: 500,
        2: 2000,
        3: 5000,
        4: 10000,
        5: 20000,
        6: 35000,
        7: 60000,
        8: 100000,
        9: 150000,
        10: null // Максимальный уровень
    };
    return thresholds[currentLevel] || null;
}

async function handleSearchClans(ws, data) {
    const requiredFields = ['search_term'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing search term');
    }

    const limit = data.limit || 20;
    const offset = data.offset || 0;
    const searchTerm = data.search_term.trim();

    if (searchTerm.length < 2) {
        return sendError(ws, 'Search term too short (min 2 characters)');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            `SELECT 
                c.clan_id,
                c.clan_name,
                c.leader_name,
                c.need_rating,
                c.is_open,
                c.clan_points,
                c.clan_level,
                COUNT(cm.player_name) as player_count,
                c.max_players,
                SUM(p.clan_points) as members_points_sum
             FROM clans c
             LEFT JOIN clan_members cm ON c.clan_id = cm.clan_id
             LEFT JOIN players p ON cm.player_id = p.id
             WHERE c.clan_name ILIKE $1
             GROUP BY c.clan_id, c.max_players, c.clan_level
             ORDER BY 
                 CASE 
                     WHEN c.clan_name ILIKE $2 THEN 0 -- Точное совпадение
                     WHEN c.clan_name ILIKE $3 THEN 1 -- Начинается с
                     ELSE 2 -- Содержит
                 END,
                 c.clan_points DESC
             LIMIT $4 OFFSET $5`,
            [
                `%${searchTerm}%`,
                `${searchTerm}`,
                `${searchTerm}%`,
                limit,
                offset
            ]
        );

        const clans = result.rows.map(clan => ({
            clan_id: clan.clan_id,
            clan_name: clan.clan_name,
            leader_name: clan.leader_name,
            need_rating: clan.need_rating,
            is_open: clan.is_open,
            player_count: clan.player_count,
            max_players: clan.max_players,
            clan_points: clan.clan_points,
            clan_level: clan.clan_level,
            points_valid: clan.clan_points === clan.members_points_sum
        }));

        ws.send(JSON.stringify({
            action: 'clan_search_results',
            success: true,
            clans: clans,
            count: clans.length,
            total: result.rowCount,
        }));

    } catch (error) {
        console.error('Clan search error:', error);
        sendError(ws, 'Search failed: ' + error.message);
    } finally {
        client.release();
    }
}
//#endregion

//#region PlayerStats
async function handleUpdatePlayerStats(ws, data) {
    const requiredFields = ['player_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required field: player_id');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Получаем текущие данные игрока (включая clan_name)
        const playerRes = await client.query(
            `SELECT 
                id,
                rating,
                money,
                donat_money,
                max_damage,
                best_rating,
                clan_name,
                clan_points
             FROM players 
             WHERE player_id = $1 
             FOR UPDATE`,
            [data.player_id]
        );

        if (playerRes.rows.length === 0) {
            throw new Error('Player not found');
        }

        const current = playerRes.rows[0];
        const newRating = (current.rating || 0) + (data.rating_change || 0);
        const newMaxDamage = Math.max(current.max_damage || 0, data.damage_dealt || 0);
        const winIncrement = data.is_win ? 1 : 0;

        // 2. Подготовка базовых параметров
        const baseParams = [
            newRating,
            data.money_change || 0,
            data.donat_money_change || 0,
            data.kills || 0,
            winIncrement,
            data.revives || 0,
            newMaxDamage,
            data.shots_fired || 0,
            data.player_id
        ];

        // 3. Строим основной запрос
        let updateQuery = `
            UPDATE players SET
                rating = $1,
                best_rating = GREATEST(best_rating, $1),
                money = money + $2,
                donat_money = donat_money + $3,
                overral_kill = overral_kill + $4,
                match_count = match_count + 1,
                win_count = win_count + $5,
                revive_count = revive_count + $6,
                max_damage = $7,
                shoot_count = shoot_count + $8
        `;

        // 4. Добавляем необязательные поля
        if (data.favorite_hero) {
            updateQuery += `, love_hero = $${baseParams.length + 1}`;
            baseParams.push(data.favorite_hero);
        }

        // 5. Обновляем клановые очки ТОЛЬКО если игрок состоит в клане
        let clanId = null;
        if (data.clan_points_change && current.clan_name) {
            updateQuery += `, clan_points = $${baseParams.length + 1}`;
            baseParams.push(data.clan_points_change);

            // Находим ID клана для обновления
            const clanRes = await client.query(
                'SELECT clan_id FROM clans WHERE clan_name = $1 FOR UPDATE',
                [current.clan_name]
            );
            
            if (clanRes.rows.length > 0) {
                clanId = clanRes.rows[0].clan_id;
                await client.query(
                    'UPDATE clans SET clan_points = $1 WHERE clan_id = $2',
                    [data.clan_points_change, clanId]
                );
            }
        }

        // 6. Завершаем запрос
        updateQuery += ` WHERE player_id = $9 RETURNING *`;

        // 7. Выполняем обновление игрока
        const updateRes = await client.query(updateQuery, baseParams);

        await client.query('COMMIT');

        // 8. Формируем ответ
        const updated = updateRes.rows[0];
        const response = {
            action: 'player_stats_updated',
            stats: {
                rating: updated.rating,
                best_rating: updated.best_rating,
                money: updated.money,
                donat_money: updated.donat_money,
                overral_kill: updated.overral_kill,
                match_count: updated.match_count,
                win_count: updated.win_count,
                revive_count: updated.revive_count,
                max_damage: updated.max_damage,
                shoot_count: updated.shoot_count,
                love_hero: updated.love_hero || null,
                clan_points: updated.clan_points_change || 0
            }
        };

        if (clanId) {
            response.clan_id = clanId;
        }

        ws.send(JSON.stringify(response));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Update player stats error:', error);
        sendError(ws, error.message.includes('love_hero') ? 
            'Invalid favorite hero data' : error.message);
    } finally {
        client.release();
    }
}
//Получение данных игрока
async function handleGetPlayerData(ws, data) {
    const requiredFields = ['player_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields: player_id');
    }

    const client = await shooterPool.connect();
    try {
        // 1. Get basic player data
        const playerResult = await client.query(
            `SELECT 
                p.player_id,
                p.player_name,
                p.rating,
                p.best_rating,
                p.money,
                p.donat_money,
                p.clan_name,
                p.clan_points,
                p.platform,
                p.open_characters,
                p.love_hero,
                p.overral_kill,
                p.match_count,
                p.win_count,
                p.revive_count,
                p.max_damage,
                p.shoot_count,
                p.friends_reward,
                p.hero_card,
                p.hero_match,
                p.hero_levels,
                c.clan_id
             FROM players p
             LEFT JOIN clans c ON p.clan_name = c.clan_name
             WHERE p.player_id = $1`,
            [data.player_id]
        );

        if (playerResult.rows.length === 0) {
            throw new Error('Player not found');
        }

        const playerData = playerResult.rows[0];

        // 2. Prepare clan data
        let clanData = null;
        if (playerData.clan_name) {
            clanData = {
                id: playerData.clan_id,
                name: playerData.clan_name,
                points: playerData.clan_points
            };
        }

        // 3. Prepare open_characters data
        let openCharacters = {};
        try {
            if (playerData.open_characters) {
                openCharacters = playerData.open_characters;
            }
        } catch (e) {
            console.error('Error parsing open_characters:', e);
            openCharacters = {"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]};
        }

        // 4. Send response
        ws.send(JSON.stringify({
            action: 'get_player_data_response',
            success: true,
            player: {
                id: playerData.player_id,
                name: playerData.player_name,
                rating: playerData.rating,
                bestRating: playerData.best_rating,
                money: playerData.money,
                donatMoney: playerData.donat_money,
                clan: clanData, // Теперь содержит clan_id
                stats: {
                    overral_kill: playerData.overral_kill,
                    matches: playerData.match_count,
                    win_count: playerData.win_count,
                    lose_count: playerData.match_count - playerData.win_count,
                    revive_count: playerData.revive_count,
                    max_damage: playerData.max_damage,
                    shoot_count: playerData.shoot_count
                },
                characters: openCharacters,
                favoriteHero: playerData.love_hero,
                platform: playerData.platform,
                friendsReward: playerData.friends_reward,
                hero_card: playerData.hero_card,
                hero_match: playerData.hero_match,
                hero_levels: playerData.hero_levels
            }
        }));

    } catch (error) {
        console.error('Get player data error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обработчик статистики героев
async function handleUpdateHeroStats(ws, data) {
    const requiredFields = ['player_id', 'hero_id', 'matches_to_add', 'hero_match'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields: player_id, hero_id, matches_to_add, hero_match');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Обновляем массив hero_match
        await client.query(
            `UPDATE players 
             SET hero_match = $1::integer[]
             WHERE player_id = $2`,
            [data.hero_match, data.player_id]
        );

        // 2. Обновляем любимого героя (если нужно)
        if (data.is_favorite) {
            await client.query(
                `UPDATE players 
                 SET love_hero = $1
                 WHERE player_id = $2`,
                [data.hero_id.toString(), data.player_id]
            );
        }

        await client.query('COMMIT');

        // 3. Получаем обновленные данные
        const result = await client.query(
            `SELECT hero_match, love_hero 
             FROM players 
             WHERE player_id = $1`,
            [data.player_id]
        );

        ws.send(JSON.stringify({
            action: 'update_hero_stats_response',
            success: true,
            hero_match: result.rows[0].hero_match,
            favorite_hero: result.rows[0].love_hero
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Update hero stats error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обработчик любимого героя
async function handleUpdateFavoriteHero(ws, data) {
    const client = await pool.connect();
    try {
        await client.query(
            'UPDATE players SET favorite_hero = $1 WHERE player_id = $2',
            [data.favorite_hero, data.player_id]
        );
    } catch (error) {
        console.error('Favorite hero update error:', error);
        sendError(ws, 'Failed to update favorite hero');
    } finally {
        client.release();
    }
}
// Обработчик Получение наград
async function handleClaimRewards(ws, data) {
    const requiredFields = ['player_id', 'money', 'donat_money', 'hero_cards', 'open_characters'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Update player's money
        await client.query(
            `UPDATE players SET 
                money = money + $1,
                donat_money = donat_money + $2
             WHERE player_id = $3`,
            [data.money, data.donat_money, data.player_id]
        );

        // 2. Update hero cards
        const heroCards = data.hero_cards;
        for (const [heroName, count] of Object.entries(heroCards)) {
            await client.query(
                `UPDATE players 
                 SET hero_card = jsonb_set(
                     COALESCE(hero_card, '{}'::jsonb),
                     $1,
                     to_jsonb(COALESCE((hero_card->>$2)::int, 0) + $3)
                 )
                 WHERE player_id = $4`,
                [`{${heroName}}`, heroName, count, data.player_id]
            );
        }

        // 3. Update open characters (unlock new heroes)
        const openCharacters = data.open_characters;
        if (openCharacters && Object.keys(openCharacters).length > 0) {
            await client.query(
                `UPDATE players 
                 SET open_characters = 
                     COALESCE(open_characters, '{}'::jsonb) || $1::jsonb
                 WHERE player_id = $2`,
                [JSON.stringify(openCharacters), data.player_id]
            );
        }

        await client.query('COMMIT');

        // 4. Get updated data for response
        const result = await client.query(
            `SELECT money, donat_money, hero_card, open_characters 
             FROM players 
             WHERE player_id = $1`,
            [data.player_id]
        );

        ws.send(JSON.stringify({
            action: 'claim_rewards_response',
            success: true,
            money: result.rows[0].money,
            donat_money: result.rows[0].donat_money,
            hero_cards: result.rows[0].hero_card,
            open_characters: result.rows[0].open_characters
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Claim rewards error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
async function handleAddCurrency(ws, data) {
    const requiredFields = ['player_id', 'money', 'donat_money'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields: player_id, money, donat_money');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Обновляем валюту игрока
        const updateResult = await client.query(
            `UPDATE players SET 
                money = money + $1,
                donat_money = donat_money + $2
             WHERE player_id = $3
             RETURNING money, donat_money`,
            [data.money, data.donat_money, data.player_id]
        );

        if (updateResult.rowCount === 0) {
            throw new Error('Player not found');
        }

        await client.query('COMMIT');

        // 2. Отправляем обновленные данные
        ws.send(JSON.stringify({
            action: 'add_currency_response',
            success: true,
            money: updateResult.rows[0].money,
            donat_money: updateResult.rows[0].donat_money
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Add currency error:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
async function handleSpendHeroCards(ws, data) {
    if (!isValidMessage(data, ['player_id', 'cards_to_spend'])) {
        return sendError(ws, 'Missing player_id or cards_to_spend');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // 1. Check if player has enough cards
        const checkResult = await client.query(
            'SELECT hero_card FROM players WHERE player_id = $1 FOR UPDATE',
            [data.player_id]
        );
        
        if (checkResult.rows.length === 0) {
            throw new Error('Player not found');
        }
        
        const currentCards = checkResult.rows[0].hero_card || {};
        const cardsToSpend = data.cards_to_spend;
        
        // 2. Validate card counts
        for (const [heroId, count] of Object.entries(cardsToSpend)) {
            const currentCount = currentCards[heroId] || 0;
            if (currentCount < count) {
                throw new Error(`Not enough cards for hero ${heroId}`);
            }
        }
        
        // 3. Update card counts
        const updatedCards = {...currentCards};
        for (const [heroId, count] of Object.entries(cardsToSpend)) {
            updatedCards[heroId] = (updatedCards[heroId] || 0) - count;
            if (updatedCards[heroId] <= 0) {
                delete updatedCards[heroId];
            }
        }
        
        // 4. Save updated cards
        await client.query(
            'UPDATE players SET hero_card = $1 WHERE player_id = $2',
            [updatedCards, data.player_id]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'spend_hero_cards_response',
            success: true,
            updated_cards: updatedCards
        }));
        
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error spending hero cards:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
async function handleGetHeroLevels(ws, data) {
    if (!isValidMessage(data, ['player_id'])) {
        return sendError(ws, 'Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            'SELECT hero_levels FROM players WHERE player_id = $1',
            [data.player_id]
        );

        ws.send(JSON.stringify({
            action: 'hero_levels_response',
            hero_levels: result.rows[0]?.hero_levels || []
        }));

    } catch (error) {
        console.error('Error getting hero levels:', error);
        sendError(ws, 'Failed to get hero levels');
    } finally {
        client.release();
    }
}
async function handleUpdateHeroLevels(ws, data) {
    const requiredFields = ['player_id', 'hero_id', 'level', 'rank'];
    if (!isValidMessage(data, requiredFields)) {
        console.log('[ERROR] Missing required fields');
        return sendError(ws, 'Missing required fields');
    }

    // Validate data
    const heroId = parseInt(data.hero_id);
    const level = parseInt(data.level);
    const rank = parseInt(data.rank);
    
    if (isNaN(heroId) || heroId < 0 || heroId > 7) {
        console.log('[ERROR] Invalid hero_id:', data.hero_id);
        return sendError(ws, 'Invalid hero_id (must be 0-7)');
    }
    
    if (isNaN(level) || level < 1 || level > 50) {
        console.log('[ERROR] Invalid level:', data.level);
        return sendError(ws, 'Invalid level (must be 1-50)');
    }
    
    if (isNaN(rank) || rank < 1 || rank > 6) {
        console.log('[ERROR] Invalid rank:', data.rank);
        return sendError(ws, 'Invalid rank (must be 1-6)');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // Corrected query using array index syntax
        const result = await client.query(`
            UPDATE players 
            SET hero_levels = jsonb_set(
                hero_levels,
                ARRAY[$1::text],
                jsonb_build_object(
                    'level', $2::int,
                    'rank', $3::int
                ),
                true
            )
            WHERE player_id = $4
            RETURNING hero_levels
        `, [heroId.toString(), level, rank, data.player_id]);

        if (result.rowCount === 0) {
            throw new Error('Player not found');
        }

        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_hero_levels_response',
            success: true,
            hero_id: heroId,
            level: level,
            rank: rank,
            hero_levels: result.rows[0].hero_levels
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('[ERROR] Update hero levels failed:', error.stack);
        
        let errorMsg = 'Failed to update hero levels';
        if (error.message.includes('invalid input syntax for type jsonb')) {
            errorMsg = 'Invalid hero levels data format';
        } else if (error.message.includes('array subscript out of range')) {
            errorMsg = 'Invalid hero_id (out of range)';
        } else if (error.message.includes('Player not found')) {
            errorMsg = 'Player not found';
        }
        
        sendError(ws, errorMsg);
    } finally {
        client.release();
    }
}
//#endregion
//#region LeaderBoard
async function handleGetRatingLeaderboard(ws, data) {
    if (!isValidMessage(data, ['player_id'])) {
        return sendError(ws, 'Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        // 1. Get top 10 players by rating (with unique places)
        const topPlayers = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    rating as value,
                    DENSE_RANK() OVER (ORDER BY rating DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY rating DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                player_id,
                name,
                value,
                unique_place as place
            FROM ranked_players
            ORDER BY unique_place
            LIMIT 10
        `);

        // 2. Get current player's position
        const myStats = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    rating as value,
                    DENSE_RANK() OVER (ORDER BY rating DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY rating DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                name,
                value,
                unique_place as place
            FROM ranked_players
            WHERE player_id = $1
        `, [data.player_id]);

        ws.send(JSON.stringify({
            action: 'rating_leaderboard_response',
            top_players: topPlayers.rows,
            my_stats: myStats.rows[0] || null
        }));

    } catch (error) {
        console.error('Error in handleGetRatingLeaderboard:', error);
        sendError(ws, 'Failed to get rating leaderboard');
    } finally {
        client.release();
    }
}
async function handleGetKillsLeaderboard(ws, data) {
    if (!isValidMessage(data, ['player_id'])) {
        return sendError(ws, 'Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        // 1. Get top 10 players by kills (with unique places)
        const topPlayers = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    overral_kill as value,
                    DENSE_RANK() OVER (ORDER BY overral_kill DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY overral_kill DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                player_id,
                name,
                value,
                unique_place as place
            FROM ranked_players
            ORDER BY unique_place
            LIMIT 10
        `);

        // 2. Get current player's position
        const myStats = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    overral_kill as value,
                    DENSE_RANK() OVER (ORDER BY overral_kill DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY overral_kill DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                name,
                value,
                unique_place as place
            FROM ranked_players
            WHERE player_id = $1
        `, [data.player_id]);

        ws.send(JSON.stringify({
            action: 'kills_leaderboard_response',
            top_players: topPlayers.rows,
            my_stats: myStats.rows[0] || null
        }));

    } catch (error) {
        console.error('Error in handleGetKillsLeaderboard:', error);
        sendError(ws, 'Failed to get kills leaderboard');
    } finally {
        client.release();
    }
}
//#endregion