import assert from 'node:assert/strict';
import { test } from 'node:test';
import { startBrowser } from '../wwwroot/startup.js';

function createLoader() {
    const error = { hidden: true, textContent: '' };
    const classes = new Set();
    const attributes = new Map();
    return {
        error,
        classes,
        attributes,
        removed: false,
        classList: { add: name => classes.add(name) },
        setAttribute: (name, value) => attributes.set(name, value),
        querySelector: selector => selector === '.boot-error' ? error : null,
        remove() { this.removed = true; },
    };
}

test('keeps the loading indicator until the app has started and painted', async () => {
    const loader = createLoader();
    let finishMain;
    const main = new Promise(resolve => { finishMain = resolve; });
    let frames = 0;
    const dotnet = {
        withApplicationArgumentsFromQuery() {
            return { create: async () => ({
                getConfig: () => ({ mainAssemblyName: 'AvaWeather.Browser' }),
                runMain: async (assembly, args) => {
                    assert.equal(assembly, 'AvaWeather.Browser');
                    assert.deepEqual(args, ['https://example.test/AvaWeather/']);
                    await main;
                },
            }) };
        },
    };

    const starting = startBrowser(async () => ({ dotnet }), {
        pageUrl: 'https://example.test/AvaWeather/',
        loader,
        nextFrame: async () => { frames++; },
    });
    await Promise.resolve();
    assert.equal(loader.removed, false);

    finishMain();
    assert.equal(await starting, true);
    assert.equal(frames, 2);
    assert.equal(loader.removed, true);
});

test('stops the indicator and exposes a startup failure', async () => {
    const loader = createLoader();
    const failure = new Error('WASM download failed');
    const reported = [];
    const result = await startBrowser(async () => { throw failure; }, {
        pageUrl: 'https://example.test/',
        loader,
        reportError: error => reported.push(error),
    });

    assert.equal(result, false);
    assert.equal(loader.removed, false);
    assert.equal(loader.classes.has('failed'), true);
    assert.equal(loader.attributes.get('aria-busy'), 'false');
    assert.equal(loader.error.hidden, false);
    assert.match(loader.error.textContent, /WASM download failed/);
    assert.deepEqual(reported, [failure]);
});
