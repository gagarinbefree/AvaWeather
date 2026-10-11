import assert from 'node:assert/strict';
import { test } from 'node:test';
import { startBrowser } from '../wwwroot/startup.js';

function createLoader() {
    const error = { hidden: true, textContent: '' };
    const progress = { textContent: '' };
    const classes = new Set();
    const attributes = new Map();
    return {
        error,
        progress,
        classes,
        attributes,
        removed: false,
        classList: { add: name => classes.add(name) },
        setAttribute: (name, value) => attributes.set(name, value),
        querySelector: selector => selector === '.boot-error' ? error : selector === '.boot-progress' ? progress : null,
        remove() { this.removed = true; },
    };
}

test('shows the assemblies currently downloading and then the application startup', async () => {
    const loader = createLoader();
    let finishCreate;
    const creating = new Promise(resolve => { finishCreate = resolve; });
    let observer;
    class ResourceObserver {
        constructor(callback) {
            this.callback = callback;
            this.disconnected = false;
            observer = this;
        }
        observe(options) { assert.deepEqual(options, { entryTypes: ['resource'] }); }
        disconnect() { this.disconnected = true; }
        finish(...urls) {
            this.callback({ getEntries: () => urls.map(name => ({ name })) });
        }
    }
    let resourceLoader;
    const runtime = {
        getConfig: () => ({ mainAssemblyName: 'AvaWeather.Browser' }),
        runMain: async () => { assert.equal(loader.progress.textContent, 'Starting application…'); },
    };
    const builder = {
        withApplicationArgumentsFromQuery() { return this; },
        withResourceLoader(callback) { resourceLoader = callback; return this; },
        create: async () => { await creating; return runtime; },
    };

    const starting = startBrowser(async () => ({ dotnet: builder }), {
        pageUrl: 'https://example.test/AvaWeather/',
        loader,
        resourceObserver: ResourceObserver,
        nextFrame: async () => {},
    });
    await Promise.resolve();
    await Promise.resolve();

    assert.equal(resourceLoader('assembly', 'Avalonia.Base.wasm', './_framework/Avalonia.Base.wasm'), undefined);
    resourceLoader('assembly', 'AvaWeather.UI.wasm', './_framework/AvaWeather.UI.wasm');
    resourceLoader('assembly', 'Application.wasm', './_framework/Application.wasm');
    resourceLoader('assembly', 'Infrastructure.wasm', './_framework/Infrastructure.wasm');
    resourceLoader('dotnetwasm', 'dotnet.native.wasm', './_framework/dotnet.native.wasm');
    assert.doesNotMatch(loader.progress.textContent, /Avalonia\.Base\.wasm/);
    assert.match(loader.progress.textContent, /AvaWeather\.UI\.wasm/);
    assert.match(loader.progress.textContent, /Application\.wasm/);
    assert.match(loader.progress.textContent, /Infrastructure\.wasm/);
    assert.doesNotMatch(loader.progress.textContent, /\(\+\d+\)|\.\.\.|…/);
    assert.doesNotMatch(loader.progress.textContent, /dotnet\.native\.wasm/);
    observer.finish('https://example.test/AvaWeather/_framework/AvaWeather.UI.wasm');
    assert.doesNotMatch(loader.progress.textContent, /AvaWeather\.UI\.wasm/);
    assert.match(loader.progress.textContent, /Application\.wasm/);

    finishCreate();
    assert.equal(await starting, true);
    assert.equal(observer.disconnected, true);
    assert.equal(loader.removed, true);
});

test('keeps the loading indicator until the app has started and painted', async () => {
    const loader = createLoader();
    let finishMain;
    const main = new Promise(resolve => { finishMain = resolve; });
    let frames = 0;
    const dotnet = {
        withApplicationArgumentsFromQuery() {
            return { withResourceLoader: () => ({ create: async () => ({
                getConfig: () => ({ mainAssemblyName: 'AvaWeather.Browser' }),
                runMain: async (assembly, args) => {
                    assert.equal(assembly, 'AvaWeather.Browser');
                    assert.deepEqual(args, ['https://example.test/AvaWeather/']);
                    await main;
                },
            }) }) };
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
