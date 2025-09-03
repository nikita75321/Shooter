var plugin = {
  //добавить игру в избранное
  VK_Star: function () {
    vkBridge.send('VKWebAppAddToFavorites')
        .then((data) => {
          if (data.result) {
            // Мини-приложение или игра добавлены в избранное
          }
        })
        .catch((error) => {
          // Ошибка
          console.log(error);
        });
  },
  //поделиться игрой не стене
  VK_Share: function () {
    vkBridge.send('VKWebAppShare', {
      link: 'https://vk.com/vkappsdev'
    })
        .then((data) => {
          if (data.result) {
            // Запись размещена
          }
        })
        .catch((error) => {
          // Ошибка
          console.log(error);
        });
  },
  //пригласить в игру
  VK_Invite: function () {
    vkBridge.send('VKWebAppShowInviteBox', {
      requestKey: "key-12345" //  Ключ приглашения
    })
        .then((data) => {
          if (data.success) {
            // Пользователь нажал «Пригласить» 
            // ...

            // Этим выбранным пользователям 
            // не удалось отправить приглашения 
            console.log('Приглашения не отправлены', data.notSentIds);
          }
        })
        .catch((error) => {
          console.log(error); // Ошибка 
        });
  },
  //баннер
  VK_Banner: function () {
    vkBridge.send('VKWebAppShowBannerAd', {banner_location: 'bottom'})
        .then((data) => {
          if (data.result) {
            console.log('Баннер показан');
            // Закрыть баннер через 30 секунд и показать новый
            setTimeout(() => {
              vkBridge.send('VKWebAppHideBannerAd').then(() => {
                console.log('Баннер скрыт');
                myGameInstance.SendMessage('Init', 'ShowBannerAd');// Показать новый баннер
              });
            }, 40000); // 30 секунд
          }
        })
        .catch((error) => {
          console.error('Ошибка при показе баннера:', error);
        });
  },

  //проверить, загрузился ли интерстишл
  VK_AdInterCheck: function () {
    vkBridge.send('VKWebAppCheckNativeAds', {ad_format: 'interstitial'});
  },
  //проверить, загрузился ли ревард
  VK_AdRewardCheck: function () {
    vkBridge.send('VKWebAppCheckNativeAds', {ad_format: 'reward'})
        .then((data) => {
          if (data.result) {
            // Предзагруженная реклама есть.

            // Теперь можно создать кнопку
            // "Прокачать героя за рекламу".   
            // ...

          } else {
            console.log('Рекламные материалы не найдены.');
          }
        })
        .catch((error) => {
          console.log(error); /* Ошибка */
        });
  },
  //показать интерстишл
  VK_Interstitial: function () {
    // Показать рекламу
    myGameInstance.SendMessage('Init', 'StopMusAndGame');
    vkBridge.send('VKWebAppShowNativeAds', {ad_format: 'interstitial'})
        .then((data) => {
          if (data.result) {
            myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
            console.log('Реклама показана');
          } else {
            myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
            console.log('Ошибка при показе');
          }
        })
        .catch((error) => {
          myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
        });
  },
  //показать ревард
  VK_Rewarded: function () {
    // Остановить музыку и игру перед показом рекламы
    myGameInstance.SendMessage('Init', 'StopMusAndGame');

    // Сбросить состояние награды
    let isRewardReady = false;

    // Показать рекламу с вознаграждением
    vkBridge.send('VKWebAppShowNativeAds', { ad_format: 'reward' })
        .then((data) => {
          if (data.result) {
            // Реклама успешно запущена, ждем завершения
            console.log('Реклама показана, ожидаем завершения...');
          } else {
            // Ошибка при показе рекламы
            myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
            console.log('Ошибка при показе рекламы');
          }
        })
        .catch((error) => {
          // Обработка ошибки
          myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
          console.error('Ошибка:', error);
        });

    // Подписываемся на событие завершения рекламы
    vkBridge.subscribe((event) => {
      if (event.detail.type === 'VKWebAppShowNativeAdsResult') {
        const result = event.detail.data.result;
        if (result) {
          // Реклама завершена, награда готова к выдаче
          isRewardReady = true;
          console.log('Реклама завершена, награда готова к выдаче');
        } else {
          // Пользователь закрыл рекламу до завершения
          console.log('Реклама не завершена, награда не выдана');
        }
        // Возобновляем музыку и игру
        myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
        if (isRewardReady === true)
        {
          myGameInstance.SendMessage('Init', 'OnRewarded');
        }
      }
    });
  },
  //только на мобилках (VK Direct)
  VK_OpenLeaderboard: function (value) {
    vkBridge.send("VKWebAppShowLeaderBoardBox", {
      user_result: value
    })
        .then((data) => {
          if (data.success) {
            // Диалоговое окно было показано
            // ...
          }
        })
        .catch((error) => {
          console.log(error); // Ошибка
        });
  },
  //для сохранения и загрузки нужно в поле ключа просто передавать JSON 
  VK_Load: function () {
    // Проверка инициализации vkBridge
    if (!vkBridge) {
      console.error('vkBridge не инициализирован');
      myGameInstance.SendMessage('Init', 'SetPlayerData', null);
      return;
    }

    // Используем метод VKWebAppStorageGet для загрузки данных
    vkBridge.send('VKWebAppStorageGet', {
      keys: ['PlayerData'], // Ключ, по которому сохраняли данные
    })
        .then((response) => {
          if (response.keys && response.keys.length > 0) {
            // Данные успешно загружены
            const savedData = response.keys[0].value; // Значение по ключу PlayerData
            console.log('Данные успешно загружены:', savedData);

            // Передаем данные в игру
            myGameInstance.SendMessage('Init', 'SetPlayerData', savedData);
          } else {
            // Данные не найдены
            console.log('Данные не найдены');
            myGameInstance.SendMessage('Init', 'SetPlayerData', null);
          }
        })
        .catch((error) => {
          // Ошибка при загрузке данных
          console.error('Ошибка при загрузке данных через VKWebAppStorageGet:', error);
          myGameInstance.SendMessage('Init', 'SetPlayerData', null);
        });
  },
  //сохраняет значение переменной, переданной в метод
  VK_Save: function (saveData) {
    const dateString = UTF8ToString(saveData);

    // Проверка инициализации vkBridge
    if (!vkBridge) {
      console.error('vkBridge не инициализирован');
      return;
    }

    // Сохраняем данные через VKWebAppStorageSet
    vkBridge.send('VKWebAppStorageSet', {
      key: 'PlayerData', // Ключ для сохранения данных
      value: dateString, // Данные для сохранения
    })
        .then((response) => {
          if (response.result) {
            console.log('Данные успешно сохранены через VKWebAppStorageSet');
          } else {
            console.error('Ошибка: данные не сохранены');
          }
        })
        .catch((error) => {
          console.error('Ошибка при сохранении данных через VKWebAppStorageSet:', error);
        });
  },
  //приглашение в сообщество
  VK_ToGroup: function () {
    vkBridge.send('VKWebAppJoinGroup', {
      group_id: 195607270
    })
        .then((data) => {
          if (data.result) {
            myGameInstance.SendMessage('Init', 'RewardForGroup');
          }
        })
        .catch((error) => {
          // Ошибка
          console.log(error);
        });
  },
  VK_RealBuy: function (item_id) {
    var id = UTF8ToString(item_id);
    vkBridge.send('VKWebAppShowOrderBox',
        {
          type: 'item', // Всегда должно быть 'item'
          item: id, // Идентификатор товара
        })
        .then((data) => {
          myGameInstance.SendMessage('Init', 'OnPurchasedItem');
        })
        .catch((e) => {
          console.log('Ошибка!', e)
        });
  },
};

mergeInto(LibraryManager.library, plugin);