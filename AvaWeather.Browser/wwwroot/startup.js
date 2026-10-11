export async function startBrowser(loadDotnet, {
    pageUrl = globalThis.location.href,
    loader = globalThis.document.getElementById('boot-status'),
    nextFrame = () => new Promise(resolve => globalThis.requestAnimationFrame(resolve)),
    reportError = error => globalThis.console.error(error),
} = {}) {
    try {
        const { dotnet } = await loadDotnet();
        const runtime = await dotnet.withApplicationArgumentsFromQuery().create();
        const config = runtime.getConfig();
        await runtime.runMain(config.mainAssemblyName, [pageUrl]);

        // Let Avalonia render before removing the loading screen.
        await nextFrame();
        await nextFrame();
        loader?.remove();
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
    }
}
