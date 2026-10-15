export async function startBrowser(loadDotnet, {
    pageUrl = globalThis.location.href,
    loader = globalThis.document.getElementById('boot-status'),
    nextFrame = () => new Promise(resolve => globalThis.requestAnimationFrame(resolve)),
    reportError = error => globalThis.console.error(error),
    resourceObserver = globalThis.PerformanceObserver,
} = {}) {
    const progressLabel = loader?.querySelector('.boot-progress-label');
    const progressAssemblies = loader?.querySelector('.boot-progress-assemblies');
    const downloading = new Map();
    let observer;
    let loadingAssemblies = true;
    const showDownloads = () => {
        if (!progressLabel || !progressAssemblies) return;
        const names = [...downloading.values()];
        const recent = names.slice(-3);
        progressLabel.textContent = recent.length ? 'Loading assemblies' : 'Loading application…';
        progressAssemblies.textContent = recent.join('\n');
    };

    try {
        const { dotnet } = await loadDotnet();
        if (resourceObserver) {
            try {
                observer = new resourceObserver(list => {
                    if (!loadingAssemblies) return;
                    for (const entry of list.getEntries()) downloading.delete(entry.name);
                    showDownloads();
                });
                observer.observe({ entryTypes: ['resource'] });
            } catch {
                observer?.disconnect();
                observer = undefined;
            }
        }
        const runtime = await dotnet.withApplicationArgumentsFromQuery()
            // The UI selects its culture after the location request. Load the Russian
            // satellite assembly up front even when the browser language is English.
            .withConfig({ loadAllSatelliteResources: true })
            .withResourceLoader((type, name, defaultUri) => {
                if (loadingAssemblies && type === 'assembly') {
                    downloading.set(new URL(defaultUri, pageUrl).href, name);
                    showDownloads();
                }
                // Keep the runtime's normal download, integrity and cache behavior.
                return undefined;
            })
            .create();
        loadingAssemblies = false;
        observer?.disconnect();
        observer = undefined;
        if (progressLabel) progressLabel.textContent = 'Starting application…';
        if (progressAssemblies) progressAssemblies.textContent = '';
        const config = runtime.getConfig();
        await runtime.runMain(config.mainAssemblyName, [pageUrl]);

        // Let Avalonia render before removing the loading screen.
        await nextFrame();
        await nextFrame();
        loader?.remove();
        globalThis.performance?.mark?.('avaweather-first-paint');
        return true;
    } catch (error) {
        reportError(error);
        if (loader) {
            loader.classList.add('failed');
            loader.setAttribute('aria-busy', 'false');
            const details = loader.querySelector('.boot-error');
            if (details) {
                details.textContent = String(error);
                details.hidden = false;
            }
        }
        return false;
    } finally {
        observer?.disconnect();
    }
}
