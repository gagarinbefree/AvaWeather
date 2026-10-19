import { startBrowser } from './startup.js';
import { registerServiceWorker } from './pwa.js';
import { prefetchCurrentPosition } from './device-location.js';

prefetchCurrentPosition();
void registerServiceWorker();
await startBrowser(() => import('./_framework/dotnet.js'));
