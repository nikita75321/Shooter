1) Поставить newtonsoft: com.unity.nuget.newtonsoft-json 
2) Вытащить на сцену префаб SocketManager
3) Привязать к InitSocket.socketConnected функции регистрации, 
загрузки и прочего, что должно произойти при подключении к сокету. Аргумент status true-подключились, 
false - ошибка
4) Пример подписки и вызова серверной функции в скрипте Example

В JS:

Отправить в unity ответ с параметрами action и server_time
ws.send(JSON.stringify({
            action: 'server_time_response',
            server_time: formatDateTime(new Date())
        }));