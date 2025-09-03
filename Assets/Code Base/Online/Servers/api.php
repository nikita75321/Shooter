<?php
header("Access-Control-Allow-Origin: *");
header("Content-Type: application/json");
header("Access-Control-Allow-Methods: POST, GET, OPTIONS");
header("Access-Control-Allow-Headers: Content-Type, Authorization");

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

try {
    $db = new PDO(
        "pgsql:host=localhost;dbname=growgarden", 
        "admin_user", 
        "RomaAndShukretSuperDevelopers",
        [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
        ]
    );
    $db->exec("SET search_path TO server_controller, public");
} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode([
        'success' => false,
        'message' => 'Database connection failed: ' . $e->getMessage()
    ]);
    exit;
}

$action = $_GET['action'] ?? '';
$input = json_decode(file_get_contents('php://input'), true) ?: [];

function getServerCpuLoad($serverUrl) {
    $serverUrl = str_replace(['wss://', 'ws://'], ['https://', 'http://'], $serverUrl);
    $serverUrl = rtrim($serverUrl, '/') . '/cpu';
    $serverHost = parse_url($serverUrl, PHP_URL_HOST);
    if ($serverHost === 'game.growagardenoffline.online') {
        $serverUrl = str_replace($serverHost, 'localhost', $serverUrl);
    }
    
    if (!parse_url($serverUrl, PHP_URL_PORT)) {
        $serverUrl = str_replace('://', '://' . '3000', $serverUrl);
    }
    
    $ch = curl_init();
    curl_setopt_array($ch, [
        CURLOPT_URL => $serverUrl,
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_TIMEOUT => 3,
        CURLOPT_SSL_VERIFYPEER => false,
        CURLOPT_SSL_VERIFYHOST => false,
        CURLOPT_FAILONERROR => true
    ]);
    
    $response = curl_exec($ch);
    $error = curl_error($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    
    if ($response === false || $httpCode !== 200) {
        error_log("CPU load request failed to {$serverUrl}. HTTP code: {$httpCode}, Error: {$error}");
        return null;
    }
    
    $data = json_decode($response, true);
    if (json_last_error() !== JSON_ERROR_NONE) {
        error_log("Invalid JSON response from {$serverUrl}: {$response}");
        return null;
    }
    
    return isset($data['cpuUsagePercent']) ? (float)$data['cpuUsagePercent'] : null;
}

function registerPlayerOnGameServer($serverUrl, $playerId) {
    $parts = explode('_', $playerId, 3);
    $platform = $parts[1] ?? 'unknown';
    $username = $parts[2] ?? $playerId;

    $serverHost = parse_url($serverUrl, PHP_URL_HOST);
    $serverHost = $serverHost === 'game.growagardenoffline.online' ? 'localhost' : $serverHost;
    
    try {
        $gameDb = new PDO(
            "pgsql:host=$serverHost;dbname=growgarden", 
            "admin_user", 
            "RomaAndShukretSuperDevelopers",
            [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]
        );
        
        $gameDb->beginTransaction();
        
        $stmt = $gameDb->prepare("
            INSERT INTO players(player_id, username, platform, last_online, money)
            VALUES(:player_id, :username, :platform, NOW(), 200)
            ON CONFLICT (player_id) DO NOTHING
        ");
        $stmt->execute([
            ':player_id' => $playerId,
            ':username' => $username,
            ':platform' => $platform
        ]);
        
        $stmt = $gameDb->prepare("
            INSERT INTO in_apps(player_id, vip_festival, the_king, upgrade_store, sale_x2, grow_x2, stock_x2)
            VALUES(:player_id, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE)
            ON CONFLICT (player_id) DO NOTHING
        ");
        $stmt->execute([':player_id' => $playerId]);
        
        $gameDb->commit();
        return true;
    } catch (PDOException $e) {
        if (isset($gameDb)) $gameDb->rollBack();
        error_log("Game server registration failed: " . $e->getMessage());
        return false;
    }
}

function player_register($player_id, $db, $is_test = false) {
    try {
        // Сначала проверяем, есть ли игрок с таким точным ID
        $stmt = $db->prepare("SELECT p.player_id, s.url, s.server_id 
                            FROM players p
                            JOIN servers s ON p.server_id = s.server_id
                            WHERE p.player_id = ?");
        $stmt->execute([$player_id]);
        $existingPlayer = $stmt->fetch();

        if ($existingPlayer) {
            // Если игрок уже существует, генерируем новый ID
            if (strpos($player_id, 'id_inkognito_') === 0 || 
                strpos($player_id, 'id_android_') === 0 ||    
                strpos($player_id, 'id_test_') === 0) {
                
                // Ищем все существующие ID с таким префиксом
                $stmt = $db->prepare("SELECT player_id FROM players WHERE player_id LIKE ? ORDER BY player_id");
                $stmt->execute([$player_id . '%']);
                $existingIds = $stmt->fetchAll(PDO::FETCH_COLUMN);
                
                $baseId = $player_id;
                $newPlayerId = $baseId;
                $counter = 1;
                
                // Генерируем новый уникальный ID
                while (in_array($newPlayerId, $existingIds)) {
                    $newPlayerId = $baseId . '_' . $counter;
                    $counter++;
                }
                
                // Теперь регистрируем с новым ID
                $query = $is_test
                    ? "SELECT server_id, url FROM servers WHERE server_name = 'testServer' ORDER BY server_id LIMIT 1"
                    : "SELECT server_id, url FROM servers WHERE is_active = TRUE AND server_name != 'testServer' ORDER BY server_id LIMIT 1";

                $server = $db->query($query)->fetch();

                if (!$server) {
                    throw new Exception("No available servers for registration");
                }

                $db->beginTransaction();
                $stmt = $db->prepare("INSERT INTO players (player_id, server_id, last_entry) VALUES (?, ?, NOW())");
                $stmt->execute([$newPlayerId, $server['server_id']]);

                if (registerPlayerOnGameServer($server['url'], $newPlayerId)) {
                    $db->commit();
                    return [
                        'success' => true,
                        'player_id' => $newPlayerId,
                        'server_url' => $server['url'],
                        'server_id' => $server['server_id']
                    ];
                }
                
                $db->rollBack();
                throw new Exception("Failed to register player on game server");
            } else {
                // Для обычных ID просто возвращаем существующего игрока
                return [
                    'success' => true,
                    'player_id' => $existingPlayer['player_id'],
                    'server_url' => $existingPlayer['url'],
                    'server_id' => $existingPlayer['server_id']
                ];
            }
        }

        // Если игрока нет вообще, регистрируем как нового
        $query = $is_test
            ? "SELECT server_id, url FROM servers WHERE server_name = 'testServer' ORDER BY server_id LIMIT 1"
            : "SELECT server_id, url FROM servers WHERE is_active = TRUE AND server_name != 'testServer' ORDER BY server_id LIMIT 1";

        $server = $db->query($query)->fetch();

        if (!$server) {
            throw new Exception("No available servers for registration");
        }

        $db->beginTransaction();
        $stmt = $db->prepare("INSERT INTO players (player_id, server_id, last_entry) VALUES (?, ?, NOW())");
        $stmt->execute([$player_id, $server['server_id']]);

        if (registerPlayerOnGameServer($server['url'], $player_id)) {
            $db->commit();
            return [
                'success' => true,
                'player_id' => $player_id,
                'server_url' => $server['url'],
                'server_id' => $server['server_id']
            ];
        }
        
        $db->rollBack();
        throw new Exception("Failed to register player on game server");

    } catch (Exception $e) {
        error_log("Registration failed: " . $e->getMessage());
        throw $e;
    }
}

function player_check_into_servers($player_id, $db, $is_test = false) {
    try {
        $query = $is_test 
            ? "SELECT server_id, url FROM servers WHERE server_name = 'testServer' ORDER BY server_id LIMIT 1"
            : "SELECT server_id, url FROM servers WHERE is_active = TRUE AND server_name != 'testServer' ORDER BY server_id";
        
        $servers = $db->query($query)->fetchAll();

        foreach ($servers as $server) {
            $serverHost = parse_url($server['url'], PHP_URL_HOST);
            $serverHost = $serverHost === 'game.growagardenoffline.online' ? 'localhost' : $serverHost;
            
            try {
                $gameDb = new PDO(
                    "pgsql:host=$serverHost;dbname=growgarden", 
                    "admin_user", 
                    "RomaAndShukretSuperDevelopers",
                    [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]
                );
                
                $stmt = $gameDb->prepare("SELECT 1 FROM players WHERE player_id = ? LIMIT 1");
                $stmt->execute([$player_id]);
                
                if ($stmt->fetchColumn()) {
                    $db->beginTransaction();
                    $stmt = $db->prepare("INSERT INTO players (player_id, server_id, last_entry) VALUES (?, ?, NOW())");
                    $stmt->execute([$player_id, $server['server_id']]);
                    $db->commit();
                    
                    return [
                        'success' => true,
                        'player_id' => $player_id,
                        'server_url' => $server['url'],
                        'server_id' => $server['server_id']
                    ];
                }
            } catch (PDOException $e) {
                error_log("Error checking server {$server['url']}: " . $e->getMessage());
                continue;
            }
        }
        
        return ['success' => false, 'message' => 'Player not found on any server'];
    } catch (Exception $e) {
        error_log("player_check_into_servers failed: " . $e->getMessage());
        throw $e;
    }
}

function player_entry($player_id, $db, $is_test = false) {
    try {
        $query = "SELECT s.url, s.server_id FROM players p
                  JOIN servers s ON p.server_id = s.server_id
                  WHERE p.player_id = ?";
        
        if ($is_test) {
            $query .= " AND s.server_name = 'testServer'";
        } else {
            $query .= " AND s.server_name != 'testServer'";
        }
        
        $stmt = $db->prepare($query);
        $stmt->execute([$player_id]);
        $server = $stmt->fetch();

        if ($server) {
            return [
                'success' => true, 
                'server_url' => $server['url'],
                'server_id' => $server['server_id']
            ];
        }
        
        $checkResult = player_check_into_servers($player_id, $db, $is_test);
        
        if ($checkResult['success']) {
            return $checkResult;
        }
        
        return player_register($player_id, $db, $is_test);
        
    } catch (Exception $e) {
        error_log("player_entry failed: " . $e->getMessage());
        throw $e;
    }
}

function user_reports($player_id, $text, $db) {
    try {
       $stmt = $db->prepare("
           SELECT created_at 
           FROM user_reports 
           WHERE player_id = ? 
           ORDER BY created_at DESC 
           LIMIT 1
       ");
       $stmt->execute([$player_id]);
       $lastReport = $stmt->fetch();
       
       if ($lastReport && strtotime($lastReport['created_at']) > time() - 3600) {
           return ['success' => false, 'message' => 'You can send only one report per hour'];
       }
        
        $stmt = $db->prepare("
            INSERT INTO user_reports (player_id, report_text, created_at)
            VALUES (?, ?, NOW())
        ");
        $stmt->execute([$player_id, $text]);
        
        return ['success' => true, 'message' => 'Report submitted successfully'];
    } catch (Exception $e) {
        error_log("Error in user_reports: " . $e->getMessage());
        throw $e;
    }
}

try {
    $is_test = filter_var($input['is_test'] ?? false, FILTER_VALIDATE_BOOLEAN);
    
    switch ($action) {
        case 'getTotalOnline':
            $servers = $db->query("SELECT url FROM servers WHERE is_active = TRUE")->fetchAll();
            $totalOnline = 0;
        
            foreach ($servers as $server) {
                try {
                    $serverUrl = str_replace('wss://', 'https://', $server['url']);
                    $serverHost = parse_url($serverUrl, PHP_URL_HOST);
                    $serverHost = ($serverHost === 'game.growagardenoffline.online') ? 'localhost' : $serverHost;
                    
                    $port = parse_url($serverUrl, PHP_URL_PORT);
                    $statsUrl = 'https://' . $serverHost . ($port ? ':' . $port : '') . '/admin/stats';
        
                    $ch = curl_init($statsUrl);
                    curl_setopt_array($ch, [
                        CURLOPT_RETURNTRANSFER => true,
                        CURLOPT_TIMEOUT => 2,
                        CURLOPT_SSL_VERIFYPEER => false,
                        CURLOPT_SSL_VERIFYHOST => false
                    ]);
                    
                    $response = curl_exec($ch);
                    if ($response === false) {
                        error_log("CURL error for {$statsUrl}: " . curl_error($ch));
                        continue;
                    }
                    
                    $data = json_decode($response, true);
                    if (isset($data['currentPlayers'])) {
                        $totalOnline += (int)$data['currentPlayers'];
                    }
                } catch (Exception $e) {
                    error_log("Error fetching stats from {$serverUrl}: " . $e->getMessage());
                    continue;
                } finally {
                    if (isset($ch)) curl_close($ch);
                }
            }
        
            echo json_encode(['totalOnline' => $totalOnline]);
            break;
            
        case 'getTotalPlayers':
            $stmt = $db->query("SELECT COUNT(*) as totalPlayers FROM players");
            $result = $stmt->fetch();
            echo json_encode(['totalPlayers' => $result['totalplayers'] ?? 0]);
            break;
            
        case 'getServers':
            $stmt = $db->query("SELECT * FROM servers ORDER BY server_id");
            echo json_encode(['servers' => $stmt->fetchAll()]);
            break;
            
        case 'getServerInfo':
            $serverId = $_GET['serverId'] ?? null;
            if (!$serverId) {
                throw new Exception('serverId parameter is required');
            }
            $stmt = $db->prepare("SELECT * FROM servers WHERE server_id = ?");
            $stmt->execute([$serverId]);
            $server = $stmt->fetch();
            if (!$server) {
                throw new Exception('Server not found');
            }
            echo json_encode($server);
            break;
            
        case 'getServerStats':
            $serverId = $_GET['serverId'] ?? null;
            if (!$serverId || !is_numeric($serverId)) {
                http_response_code(400);
                echo json_encode(['success' => false, 'message' => 'Invalid serverId parameter']);
                break;
            }
            
            try {
                $stmt = $db->prepare("SELECT server_name FROM servers WHERE server_id = ?");
                $stmt->execute([$serverId]);
                $server = $stmt->fetch();
                
                if (!$server) {
                    http_response_code(404);
                    echo json_encode(['success' => false, 'message' => 'Server not found']);
                    break;
                }
                
                $stmt = $db->prepare("
                    SELECT *, 
                           active_players_peak AS peak_players,
                           total_connections,
                           unique_players
                    FROM server_controller.online_stats_v2 
                    WHERE server_name = ?
                    ORDER BY date DESC 
                    LIMIT 30
                ");
                $stmt->execute([$server['server_name']]);
                $stats = $stmt->fetchAll();
                
                if (empty($stats)) {
                    http_response_code(404);
                    echo json_encode(['success' => false, 'message' => 'No stats found for this server']);
                } else {
                    echo json_encode($stats);
                }
            } catch (PDOException $e) {
                error_log("Database error in getServerStats: " . $e->getMessage());
                http_response_code(500);
                echo json_encode([
                    'success' => false, 
                    'message' => 'Database error',
                    'error_details' => $e->getMessage()
                ]);
            }
            break;
            
        case 'getUserReports':
            $stmt = $db->query("SELECT * FROM user_reports ORDER BY created_at DESC");
            echo json_encode(['reports' => $stmt->fetchAll()]);
            break;
            
        case 'getUniquePlayersToday':
            try {
                $stmt = $db->query("
                    SELECT SUM(unique_players) as total_unique 
                    FROM server_controller.online_stats_v2 
                    WHERE date::date = CURRENT_DATE
                ");
                $result = $stmt->fetch();
                echo json_encode(['totalUnique' => $result['total_unique'] ?? 0]);
            } catch (PDOException $e) {
                error_log("Database error in getUniquePlayersToday: " . $e->getMessage());
                http_response_code(500);
                echo json_encode(['success' => false, 'message' => 'Database error']);
            }
            break;
            
        case 'player_register':
            if (empty($input['player_id'])) {
                throw new Exception('player_id is required');
            }
            $response = player_register($input['player_id'], $db, $is_test);
            echo json_encode($response);
            break;
            
        case 'player_entry':
            if (empty($input['player_id'])) {
                throw new Exception('player_id is required');
            }
            $response = player_entry($input['player_id'], $db, $is_test);
            echo json_encode($response);
            break;
            
        case 'user_reports':
            if (empty($input['player_id']) || empty($input['text'])) {
                throw new Exception('Both player_id and text are required');
            }
            $response = user_reports($input['player_id'], $input['text'], $db);
            echo json_encode($response);
            break;
            
        case 'deleteReport':
            if (empty($input['reportId'])) {
                http_response_code(400);
                echo json_encode(['success' => false, 'message' => 'reportId is required']);
                break;
            }
            
            try {
                $stmt = $db->prepare("DELETE FROM user_reports WHERE report_id = ?");
                $stmt->execute([$input['reportId']]);
                
                if ($stmt->rowCount() > 0) {
                    echo json_encode(['success' => true, 'message' => 'Report deleted successfully']);
                } else {
                    http_response_code(404);
                    echo json_encode(['success' => false, 'message' => 'Report not found']);
                }
            } catch (PDOException $e) {
                http_response_code(500);
                echo json_encode([
                    'success' => false,
                    'message' => 'Database error',
                    'error' => $e->getMessage()
                ]);
            }
            break;
                     
        case 'getServerCpuUsage':
            $serverId = $_GET['serverId'] ?? null;
            if (!$serverId) {
                throw new Exception('serverId parameter is required');
            }
                        
            $stmt = $db->prepare("SELECT url FROM servers WHERE server_id = ?");
            $stmt->execute([$serverId]);
            $server = $stmt->fetch();
                        
            if (!$server) {
                throw new Exception('Server not found');
            }
                        
            $cpuUsage = getServerCpuLoad($server['url']);
            echo json_encode(['cpuUsage' => $cpuUsage ?? 0]);
            break;
            
        default:
            http_response_code(404);
            echo json_encode(['success' => false, 'message' => 'Action not found']);
            break;
    }
    
} catch (Exception $e) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => $e->getMessage()
    ]);
}
?>