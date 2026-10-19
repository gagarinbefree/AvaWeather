const cacheKey = 'avaweather.forecast.v1';

export function readWeatherCache() {
    try {
        return globalThis.localStorage.getItem(cacheKey);
    } catch {
        return null;
    }
}

export function writeWeatherCache(value) {
    try {
        globalThis.localStorage.setItem(cacheKey, value);
    } catch {
        // Storage can be unavailable in private browsing or when the quota is exhausted.
    }
}
