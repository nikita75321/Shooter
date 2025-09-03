mergeInto(LibraryManager.library, {
    CrazyGames_GetUserId: function() {
        try {
            if (typeof window.CrazyGames === 'undefined') {
                console.warn("[CrazyGames] SDK not detected");
                sendFallbackId('sdk_not_loaded');
                return;
            }

            let attempts = 0;
            const maxAttempts = 30;

            const checkSDKReady = () => {
                attempts++;
                if (window.CrazyGames.SDK && window.CrazyGames.SDK.user) {
                    getUserIdFromSDK();
                } else if (attempts < maxAttempts) {
                    setTimeout(checkSDKReady, 100);
                } else {
                    console.warn("[CrazyGames] SDK initialization timeout");
                    sendFallbackId('sdk_init_timeout');
                }
            };

            const getUserIdFromSDK = () => {
                if (!window.CrazyGames.SDK.user.isUserAccountAvailable) {
                    console.warn("[CrazyGames] Account system unavailable");
                    sendFallbackId('account_system_unavailable');
                    return;
                }

                window.CrazyGames.SDK.user.getUserToken()
                    .then(token => {
                        try {
                            if (!token) throw new Error("Empty token received");
                            const payload = parseJwt(token);
                            if (payload && payload.userId) {
                                sendUserId(payload.userId);
                            } else {
                                throw new Error("No userId in token");
                            }
                        } catch (e) {
                            console.warn("[CrazyGames] Token parse failed, trying username");
                            getUsernameFallback();
                        }
                    })
                    .catch(e => {
                        console.warn("[CrazyGames] Token request failed:", e);
                        getUsernameFallback();
                    });
            };

            const getUsernameFallback = () => {
                window.CrazyGames.SDK.user.getUser()
                    .then(user => {
                        if (user && user.username) {
                            sendUserId(user.username);
                        } else {
                            throw new Error("No username available");
                        }
                    })
                    .catch(e => {
                        console.warn("[CrazyGames] Username fallback failed:", e);
                        sendFallbackId('username_fallback_failed');
                    });
            };

            const parseJwt = (token) => {
                try {
                    const base64Url = token.split('.')[1];
                    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                    return JSON.parse(decodeURIComponent(escape(atob(base64))));
                } catch (e) {
                    console.error("JWT parse error:", e);
                    return null;
                }
            };

            const generateGuestId = () => {
                return 'guest_' + Math.random().toString(36).substr(2, 9);
            };

            const sendUserId = (id) => {
                try {
                    SendMessage('Init', 'OnUserIdReceived', id);
                } catch (e) {
                    console.error("SendMessage failed:", e);
                }
            };

            const sendFallbackId = (reason) => {
                const guestId = generateGuestId();
                console.warn(`Using fallback ID (${reason}): ${guestId}`);
                try {
                    SendMessage('Init', 'OnUserIdError', reason);
                    SendMessage('Init', 'OnUserIdReceived', guestId);
                } catch (e) {
                    console.error("Fallback SendMessage failed:", e);
                }
            };

            checkSDKReady();

        } catch (e) {
            console.error("[CrazyGames] Initialization error:", e);
            try {
                SendMessage('Init', 'OnUserIdError', 'initialization_error');
            } catch (_) {}
        }
    }
});
