import { startBrowser } from './startup.js';
import { registerServiceWorker } from './pwa.js';

void registerServiceWorker();
await startBrowser(() => import('./_framework/dotnet.js'));
