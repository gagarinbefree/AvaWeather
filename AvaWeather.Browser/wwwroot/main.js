import { startBrowser } from './startup.js';

await startBrowser(() => import('./_framework/dotnet.js'));
