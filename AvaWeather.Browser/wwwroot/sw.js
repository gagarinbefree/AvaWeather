const cacheName = 'avaweather-offline-__OFFLINE_VERSION__';
const cachePrefix = 'avaweather-offline-';
const scope = self.registration.scope;
const offlineUrl = new URL('offline.html', scope).href;
const indexUrl = new URL('index.html', scope).href;
const manifestUrl = new URL('offline-assets.json', scope).href;

// Cache only the published application files. API requests and appsettings are excluded.
self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const response = await fetch(manifestUrl, { cache: 'no-store' });
        if (!response.ok) throw new Error('Offline asset manifest unavailable');
        const paths = await response.json();
        if (!Array.isArray(paths)) throw new Error('Invalid offline asset manifest');
        const urls = paths.map(path => new URL(path, scope));
        if (urls.some(url => !url.href.startsWith(scope) || url.search || url.hash ||
            url.pathname.endsWith('/appsettings.json'))) throw new Error('Invalid offline asset');
        const cache = await caches.open(cacheName);
        await cache.addAll(urls.map(url => url.href));
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names.filter(name => name.startsWith(cachePrefix) && name !== cacheName)
            .map(name => caches.delete(name)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(scope)) return;
    const url = new URL(request.url);
    if (url.pathname.endsWith('/appsettings.json')) {
        event.respondWith((async () => {
            try { return await fetch(request); }
            catch { return new Response('{}', { headers: { 'Content-Type': 'application/json' } }); }
        })());
        return;
    }
    if (url.search && request.mode !== 'navigate') return;
    if (request.mode === 'navigate') {
        event.respondWith((async () => {
            try { return await fetch(request); }
            catch {
                const cache = await caches.open(cacheName);
                const savedPage = await cache.match(url.href.split('?')[0]);
                if (savedPage) return savedPage;
                if (url.href.split('?')[0] === scope) {
                    const index = await cache.match(indexUrl);
                    if (index) return index;
                }
                return await cache.match(offlineUrl) || Response.error();
            }
        })());
        return;
    }
    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        return await cache.match(request) || await fetch(request);
    })());
});
