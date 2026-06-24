window.repyPharmaAuthStorage = {
    get: function (storageName, key) {
        return window[storageName].getItem(key);
    },
    set: function (storageName, key, value) {
        window[storageName].setItem(key, value);
    },
    clear: function (key) {
        sessionStorage.removeItem(key);
        localStorage.removeItem(key);
    }
};
