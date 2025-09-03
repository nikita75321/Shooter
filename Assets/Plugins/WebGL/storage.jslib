mergeInto(LibraryManager.library, {
    SaveToLocalStorage: function(key, data) {
        var strKey = UTF8ToString(key);
        var strData = UTF8ToString(data);
        try {
            localStorage.setItem(strKey, strData);
        } catch(e) {
            console.error("LocalStorage save error:", e);
        }
    },

    LoadFromLocalStorage: function(key) {
        var strKey = UTF8ToString(key);
        try {
            var data = localStorage.getItem(strKey);
            if (data) {
                var buffer = _malloc(data.length + 1);
                stringToUTF8(data, buffer, data.length + 1);
                return buffer;
            }
            return 0;
        } catch(e) {
            console.error("LocalStorage load error:", e);
            return 0;
        }
    }
});