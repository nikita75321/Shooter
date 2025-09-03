const { Pool } = require('pg');

class Database {
    constructor() {
        this.pool = new Pool({
            user: 'admin_user',
            host: 'localhost',
            database: 'shooter', //УКАЗАТЬ НАЗВАНИЕ БД
            password: 'RomaAndShukretSuperDevelopers', //УКАЗАТЬ ПАРОЛЬ БД
            port: 5432,
            max: 20, 
            idleTimeoutMillis: 30000,
            connectionTimeoutMillis: 5000, 
            application_name: 'tds', //УКАЗАТЬ НАЗВАНИЕ ИГРЫ
            allowExitOnIdle: true,
            maxUses: 7500, 
        });

        this.setupEventListeners();
    }

    setupEventListeners() {
        this.pool.on('connect', () => {
            console.log('New client connected to the pool');
        });

        this.pool.on('error', (err) => {
            console.error('Unexpected error on idle client', err);
        });
    }

    // В классе Database добавьте:
    async query(text, params, timeout = 5000) {
        const client = await this.pool.connect();
        try {
            await client.query(`SET LOCAL statement_timeout = ${timeout}`);
            return await client.query(text, params);
        } finally {
            client.release();
        }
    }
    
// В класс Database добавьте:
    async transaction(callback) {
        const client = await this.pool.connect();
        try {
            await client.query('BEGIN');
            const result = await callback(client); // Передаем client в callback
            await client.query('COMMIT');
            return result;
        } catch (error) {
            await client.query('ROLLBACK');
            throw error;
        } finally {
            client.release();
        }
    }
    
    async connect() {
        return this.pool.connect();
    }

    async close() {
        await this.pool.end();
    }
}

const dbInstance = new Database();

// Add initialization function
async function initializeDatabase() {
    try {
        // Test connection
        const client = await dbInstance.connect();
        await client.query('SELECT 1');
        client.release();
        console.log('Database connection established successfully');
    } catch (err) {
        console.error('Database connection failed:', err);
        throw err;
    }
}

module.exports = {
    shooterPool: dbInstance,
    db: dbInstance,
    initializeDatabase
};