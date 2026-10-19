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

test('uses cached application files offline without caching weather requests or settings', async () => {
    const listeners = new Map();
    const offline = { page: 'offline' };
    const index = { page: 'app' };
    const assembly = { file: 'assembly' };
    const cached = new Map([
        ['https://example.test/AvaWeather/offline.html', offline],
        ['https://example.test/AvaWeather/index.html', index],
        ['https://example.test/AvaWeather/_framework/app.wasm', assembly],
    ]);
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
        match: async request => cached.get(typeof request === 'string' ? request : request.url),
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
        fetch: async url => {
            if (url === 'https://example.test/AvaWeather/offline-assets.json') return {
                ok: true, json: async () => ['index.html', 'offline.html', '_framework/app.wasm'],
            };
            throw new TypeError('offline');
        },
        URL,
        Response,
    });

    let installing;
    listeners.get('install')({ waitUntil(promise) { installing = promise; } });
    await installing;
    assert.deepEqual(precached, [
        'https://example.test/AvaWeather/index.html',
        'https://example.test/AvaWeather/offline.html',
        'https://example.test/AvaWeather/_framework/app.wasm',
    ]);

    let response;
    listeners.get('fetch')({
        request: { method: 'GET', mode: 'navigate', url: 'https://example.test/AvaWeather/' },
        respondWith(promise) { response = promise; },
    });
    assert.equal(await response, index);

    listeners.get('fetch')({
        request: { method: 'GET', mode: 'cors', url: 'https://example.test/AvaWeather/_framework/app.wasm' },
        respondWith(promise) { response = promise; },
    });
    assert.equal(await response, assembly);

    listeners.get('fetch')({
        request: { method: 'GET', mode: 'cors', url: 'https://example.test/AvaWeather/appsettings.json' },
        respondWith(promise) { response = promise; },
    });
    assert.deepEqual(await (await response).json(), {});

    for (const url of ['https://api.weatherapi.com/v1/forecast.json?key=secret']) {
        response = undefined;
        listeners.get('fetch')({ request: { method: 'GET', mode: 'cors', url },
            respondWith(promise) { response = promise; } });
        assert.equal(response, undefined);
    }

    response = undefined;
    listeners.get('fetch')({
        request: { method: 'GET', mode: 'navigate', url: 'https://example.test/another-site/' },
        respondWith(promise) { response = promise; },
    });
    assert.equal(response, undefined);
});
