import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import vm from 'node:vm';
import { registerServiceWorker } from '../wwwroot/pwa.js';

test('registers the service worker within the current application subpath', async () => {
    const registrations = [];
    const serviceWorker = {
        async register(script, options) { registrations.push({ script, options }); },
    };

    assert.equal(await registerServiceWorker(serviceWorker,
        'https://example.test/AvaWeather/sw.js'), true);
    assert.deepEqual(registrations, [{
        script: 'https://example.test/AvaWeather/sw.js',
        options: { scope: 'https://example.test/AvaWeather/' },
    }]);
    assert.equal(await registerServiceWorker(null), false);
});

test('returns the offline page when an in-scope navigation fails', async () => {
    const listeners = new Map();
    const offline = { page: 'offline' };
    const cached = new Map([['https://example.test/AvaWeather/offline.html', offline]]);
    const serviceWorker = {
        location: { origin: 'https://example.test', href: 'https://example.test/AvaWeather/sw.js' },
        registration: { scope: 'https://example.test/AvaWeather/' },
        addEventListener(type, listener) { listeners.set(type, listener); },
        skipWaiting() {},
        clients: { claim() {} },
    };
    const precached = [];
    const cache = {
        addAll: async urls => { precached.push(...urls); },
        match: async url => cached.get(url),
    };
    const caches = {
        open: async () => cache,
        keys: async () => [],
        delete: async () => true,
    };
    const source = await readFile(new URL('../wwwroot/sw.js', import.meta.url), 'utf8');
    vm.runInNewContext(source, {
        self: serviceWorker,
        caches,
        fetch: async () => { throw new TypeError('offline'); },
        URL,
    });

    let installing;
    listeners.get('install')({ waitUntil(promise) { installing = promise; } });
    await installing;
    assert.deepEqual(precached, [
        'https://example.test/AvaWeather/offline.html',
        'https://example.test/AvaWeather/icons/app-icon-192.png',
    ]);

    let response;
    listeners.get('fetch')({
        request: { mode: 'navigate', url: 'https://example.test/AvaWeather/' },
        respondWith(promise) { response = promise; },
    });
    assert.equal(await response, offline);

    response = undefined;
    listeners.get('fetch')({
        request: { mode: 'navigate', url: 'https://example.test/another-site/' },
        respondWith(promise) { response = promise; },
    });
    assert.equal(response, undefined);
});
