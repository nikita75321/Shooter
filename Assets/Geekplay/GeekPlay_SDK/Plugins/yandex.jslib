var plugin = {

  /////MAIN/////
  GamePlatform : function()
    {
        console.log(location.hostname);
        p = UTF8ToString(location.hostname);
        myGameInstance.SendMessage('Init', 'ChangePlatform', p);
    },
  /////MAIN/////

  DownloadFile : function(array, size, fileNamePtr)
  {
    var fileName = UTF8ToString(fileNamePtr);
 
    var bytes = new Uint8Array(size);
    for (var i = 0; i < size; i++)
    {
       bytes[i] = HEAPU8[array + i];
    }
 
    var blob = new Blob([bytes]);
    var link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
 
    var event = document.createEvent("MouseEvents");
    event.initMouseEvent("click");
    link.dispatchEvent(event);
    window.URL.revokeObjectURL(link.href);
  }, 


  /////YANDEX//////
    GameStart : function()
    {
       ysdk.features.LoadingAPI.ready();
    },
    
    GameReady : function()
    {
      if(ysdk.features.GameplayAPI)
      {       
         ysdk.features.GameplayAPI.start();
      }
    },
   
    GameStop : function()
    {
      if(ysdk.features.GameplayAPI)
      {       
         ysdk.features.GameplayAPI.stop();
      }
    },

    IsMobile : function()
    {
        if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
            myGameInstance.SendMessage('Init', 'ItIsMobile');
        }
    },

    RateGame: function () {
        ysdk.feedback.canReview()
        .then(({ value, reason }) => {
            if (value) {
            	myGameInstance.SendMessage('Init', 'StopMusAndGame');
                ysdk.feedback.requestReview()
                    .then(({ feedbackSent }) => {
                    	myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
                        console.log(feedbackSent);
                    })
            } else {
            	myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
                console.log(reason)
            }
        })
    },

    SaveExtern: function(date) {
        var dateString = UTF8ToString(date);
        var myobj = JSON.parse(dateString);
        player.setData(myobj);
      },

    LoadExtern: function(){
        player.getData().then(_date => {
            //const myJSON = JSON.stringify(_date);
            const myJSON = UTF8ToString(_date);
            myGameInstance.SendMessage('Init', 'SetPlayerData', myJSON);
        });
    },  	
    
    AdInterstitial : function () {
        ysdk.adv.showFullscreenAdv({
          callbacks: {
        onOpen: function(wasShown) {
	  myGameInstance.SendMessage('Init', 'InterAdStart');
          myGameInstance.SendMessage('Init', 'StopMusAndGame');
          myGameInstance.SendMessage('Init', 'plusAds');
         if(ysdk.features.GameplayAPI)
      {       
         ysdk.features.GameplayAPI.stop();
      }
        },
        onClose: function(wasShown) {
	  myGameInstance.SendMessage('Init', 'InterAdStop');
          myGameInstance.SendMessage('Init', 'ResumeMusAndGame');
        },
        onError: function(error) {
          // some action on error
        }
        }
    });
    },

    AdReward : function(){
        ysdk.adv.showRewardedVideo({
        callbacks: {
        onOpen: () => {
	  myGameInstance.SendMessage('Init', 'RewardAdStart');
          console.log('Video ad open.');
          myGameInstance.SendMessage('Init', 'StopMusAndGame');
          if(ysdk.features.GameplayAPI)
      {       
         ysdk.features.GameplayAPI.stop();
      }

        },
        onRewarded: () => {
          myGameInstance.SendMessage('Init', 'OnRewarded');
        },
        onClose: () => {
          console.log('Video ad closed.');
	  myGameInstance.SendMessage('Init', 'RewardAdStop');
          myGameInstance.SendMessage('Init', 'ResumeMusAndGame');

        }, 
        onError: (e) => {
          console.log('Error while open video ad:', e);
        }
    }
    });
    },

    GetLang : function(){
      var lang = ysdk.environment.i18n.lang;
      var bufferSize = lengthBytesUTF8(lang) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(lang, buffer, bufferSize);
      return buffer;
    }, 

    SetToLeaderboard : function(value, leaderboardName){
          leaderboardName = UTF8ToString(leaderboardName);
          ysdk.getLeaderboards()
            .then(lb => {
          // Без extraData
          lb.setLeaderboardScore(leaderboardName, value);
          });
    },

    GetDomain: function() {
      var lang = ysdk.environment.i18n.tld;
      var bufferSize = lengthBytesUTF8(lang) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(lang, buffer, bufferSize);
      return buffer;
    },

    

    CheckPlayGame: function (id) {
       
        player.getIDsPerGame()
          .then(res => {
            console.log(res);
            console.log(res.length);

            for (let i = 0; i < res.length; i++)
            {
                console.log(res[i].appID);
                console.log(i);

                if(id == res[i].appID)
                {
                    console.log("true");
                    myGameInstance.SendMessage('Init', 'EnablePlayedGameToggle', id);
                    return "true";
                }
            }   
            myGameInstance.SendMessage('Init', 'DisablePlayedGameToggle', id);
          });

        return "false";
    },

    BuyItem : function (idOrTag, d) {
    	idOrTag = UTF8ToString(idOrTag);
      var dateString = UTF8ToString(d);
      var myobj = JSON.parse(dateString);
      player.setData(myobj);
    	ysdk.getPayments({ signed: true }).then(_payments => {
        	payments = _payments;
        	payments.purchase(idOrTag).then(purchase => {
        		myGameInstance.SendMessage('Init', 'OnPurchasedItem');
        		payments.consumePurchase(purchase.purchaseToken); //для разовых покупок
        		window.focus();
    		})
	    	}).catch(err => {
	        	console.alert(err);
	   	}).catch(err => {
	        console.alert(err);
	    })
    },
    CheckBuyItem: function (idOrTag) {
      	idOrTag = UTF8ToString(idOrTag);
        console.log(idOrTag);

        payments.getPurchases().then(purchases => purchases.forEach(consumePurchase));


  		  payments.getPurchases().then(purchases => {
  		    if (purchases.some(purchase => purchase.productID === idOrTag)) {
  		    	myGameInstance.SendMessage('Init', 'SetPurchasedItem');
  		    }
          else
          {
            myGameInstance.SendMessage('Init', 'NotSetPurchasedItem');
          }
  		  }).catch(err => {
  		    // Выбрасывает исключение USER_NOT_AUTHORIZED для неавторизованных пользователей.
  		  })
	  },

    GetLeaderboard: function (type, number, name) {
      type = UTF8ToString(type);
      console.log(type);

      name = UTF8ToString(name);
      console.log(name);

          ysdk.getLeaderboards()
      .then(lb => {
        // Получение 10 топов
        lb.getLeaderboardEntries(name, { quantityTop: 10 })
          .then(res => {
            console.log(res);
            if (res.entries.length <= number)
            {
              //myGameInstance.SendMessage('Init', 'EndGetLeaderboardsValue');
              console.log("NULL");
              return;
            }
            else if (type == "score")
            {
              console.log("SCORE");              
              console.log(String(res.entries[number].score));
              var message = String(res.entries[number].score);
              if (name == "Points")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersScore', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders3', message);
              }
            }
            else if (type == "name")
            {
              console.log("NAME");
              console.log(String(res.entries[number].player.publicName))
              var message = String(res.entries[number].player.publicName);
              if (name == "Points")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName3', message);
              }
            }
          });
      });
    },

    GetLeaderboard2: function (type, number, name) {
      type = UTF8ToString(type);
      console.log(type);

      name = UTF8ToString(name);
      console.log(name);

          ysdk.getLeaderboards()
      .then(lb => {
        // Получение 10 топов
        lb.getLeaderboardEntries(name, { quantityTop: 10 })
          .then(res => {
            console.log(res);
            if (res.entries.length <= number)
            {
              //myGameInstance.SendMessage('Init', 'EndGetLeaderboardsValue');
              console.log("NULL");
              return;
            }
            else if (type == "score")
            {
              console.log("SCORE");              
              console.log(String(res.entries[number].score));
              var message = String(res.entries[number].score);
              if (name == "Build")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders3', message);
              }
            }
            else if (type == "name")
            {
              console.log("NAME");
              console.log(String(res.entries[number].player.publicName))
              var message = String(res.entries[number].player.publicName);
              if (name == "Build")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName3', message);
              }
            }
          });
      });
    },

    GetLeaderboard3: function (type, number, name) {
      type = UTF8ToString(type);
      console.log(type);

      name = UTF8ToString(name);
      console.log(name);

          ysdk.getLeaderboards()
      .then(lb => {
        // Получение 10 топов
        lb.getLeaderboardEntries(name, { quantityTop: 10 })
          .then(res => {
            console.log(res);
            if (res.entries.length <= number)
            {
              //myGameInstance.SendMessage('Init', 'EndGetLeaderboardsValue');
              console.log("NULL");
              return;
            }
            else if (type == "score")
            {
              console.log("SCORE");              
              console.log(String(res.entries[number].score));
              var message = String(res.entries[number].score);
              if (name == "Build")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeaders3', message);
              }
            }
            else if (type == "name")
            {
              console.log("NAME");
              console.log(String(res.entries[number].player.publicName))
              var message = String(res.entries[number].player.publicName);
              if (name == "Build")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName', message);
              }
              if (name == "Destroy")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName2', message);
              }
              if (name == "Donat")
              {
                myGameInstance.SendMessage('Init', 'GetLeadersName3', message);
              }
            }
          });
      });
    },
    GetValueCode: function()
    {
      console.log(gameShop[0].priceCurrencyCode);
      if (gameShop[0].priceCurrencyCode === "TST" && gameShop[0].priceCurrencyCode === 'TST')
      {
        myGameInstance.SendMessage('Init', 'ChangeYanType');
        console.log("TYPEEE " + gameShop[0].priceCurrencyCode);
      }
    },

    GetMyValueLeaderboard: function()
    {
    ysdk.getLeaderboards()
      .then(lb => lb.getLeaderboardPlayerEntry('Points'))
      .then(res => 
      {
        console.log(res);
        myGameInstance.SendMessage('Init', 'SetMyScore', res.score);
        myGameInstance.SendMessage('Init', 'SetMyPlace', res.rank);
        return res;
      }).catch(err => {
      if (err.code === 'LEADERBOARD_PLAYER_NOT_PRESENT') {
      // Срабатывает, если у игрока нет записи в лидерборде.
      }
      });
    },

    CheckPlayGame: function (id) {
       
        player.getIDsPerGame()
          .then(res => {
            console.log(res);
            console.log(res.length);

            for (let i = 0; i < res.length; i++)
            {
                console.log(res[i].appID);
                console.log(i);

                if(id == res[i].appID)
                {
                    console.log("true");
                    myGameInstance.SendMessage('Init', 'GamePlayed', id);
                    return "true";
                }
            }   
            myGameInstance.SendMessage('Init', 'GameNotPlayed', id);
          })
          .catch((err) => {
            myGameInstance.SendMessage('Init', 'GameNotPlayed', id);
          });

        return "false";
    },

    GetAllGames: function () {
      ysdk.features.GamesAPI.getAllGames().then(({games, developerURL}) => {
        myGameInstance.SendMessage('Init', 'OpenAllGames', developerURL);
      //games.forEach((game) => {
      //        myGameInstance.SendMessage('Init', 'OpenAllGames', developerURL);
      //    })
      }).catch(err => {
          // Ошибка при получении данных об игре.
      })
    },

    GetGameByID: function (id) {
      ysdk.features.GamesAPI.getGameByID(id).then(({isAvailable, game}) => {
      if (isAvailable) {
          myGameInstance.SendMessage('Init', 'OpenGame', game.url);
      } else {
          // Логика, если игра недоступна, объект game не определен (undefined).
      }
      }).catch(err => {
          // Ошибка при получении данных об игре.
      })
    },

  /////YANDEX//////

  GetMicrophone: function() {
	  navigator.mediaDevices
	    .getUserMedia({ video: false, audio: true })
	    .then((stream) => {
	      window.localStream = stream; // A
	      window.localAudio.srcObject = stream; // B
	      window.localAudio.autoplay = true; // C
	    })
	    .catch((err) => {
	      console.error(`you got an error: ${err}`);
	    });
	},

    ShowBanner: function() {
        ysdk.adv.showBannerAdv();
    },

    CloseBanner: function() {
        ysdk.adv.hideBannerAdv();
    },
};

mergeInto(LibraryManager.library, plugin);