const cacheName = 'avaweather-offline-v1';
const cachePrefix = 'avaweather-offline-';
const scope = self.registration.scope;
const offlineUrl = new URL('offline.html', scope).href;

// Keep weather responses and their API-key URLs out of offline storage.
// A forecast shown after reconnection must come from the live API.

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(cacheName)
            .then(cache => cache.addAll([offlineUrl, new URL('icons/app-icon-192.png', scope).href]))
            .then(() => self.skipWaiting())
    );
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
    if (event.request.mode !== 'navigate' ||
        !event.request.url.startsWith(scope)) return;

    event.respondWith((async () => {
        try {
            return await fetch(event.request);
        } catch {
            const cache = await caches.open(cacheName);
            return await cache.match(offlineUrl) || Response.error();
        }
    })());
});
