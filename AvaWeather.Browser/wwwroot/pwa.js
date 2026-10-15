export async function registerServiceWorker(
    serviceWorker = globalThis.navigator?.serviceWorker,
    scriptUrl = new URL('./sw.js', import.meta.url).href,
) {
    if (!serviceWorker) return false;
    try {
        const scope = new URL('./', scriptUrl).href;
        await serviceWorker.register(scriptUrl, { scope });
        return true;
    } catch (error) {
        globalThis.console.warn('AvaWeather offline support is unavailable', error);
        return false;
    }
}
