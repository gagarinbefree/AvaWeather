// @ts-nocheck
import assert from 'node:assert/strict';
import { getCurrentPosition, prefetchCurrentPosition } from '../wwwroot/device-location.js';

Object.defineProperty(globalThis, 'isSecureContext', { configurable: true, value: true });
let options;
Object.defineProperty(globalThis, 'navigator', {
    configurable: true,
    value: { geolocation: {
        getCurrentPosition(success, _error, requestedOptions) {
            options = requestedOptions;
            success({ coords: { latitude: 58.0047, longitude: 56.2514 } });
        }
    } }
});
assert.equal(await getCurrentPosition(), '58.0047,56.2514');
assert.ok(options.timeout > 0);
assert.ok(options.maximumAge > 0);

Object.defineProperty(globalThis, 'navigator', {
    configurable: true,
    value: { geolocation: { getCurrentPosition(_success, error) { error({ code: 1 }); } } }
});
assert.equal(await getCurrentPosition(), null);

Object.defineProperty(globalThis, 'navigator', { configurable: true, value: {} });
assert.equal(await getCurrentPosition(), null);
Object.defineProperty(globalThis, 'isSecureContext', { configurable: true, value: false });
assert.equal(await getCurrentPosition(), null);

Object.defineProperty(globalThis, 'isSecureContext', { configurable: true, value: true });
let requests = 0;
let finishPosition;
Object.defineProperty(globalThis, 'navigator', {
    configurable: true,
    value: { geolocation: { getCurrentPosition(success) { requests++; finishPosition = success; } } }
});
prefetchCurrentPosition();
prefetchCurrentPosition();
assert.equal(requests, 1, 'startup should begin exactly one location request');
const initialPosition = getCurrentPosition();
assert.equal(requests, 1, 'the application should reuse the startup request');
finishPosition({ coords: { latitude: 58, longitude: 56 } });
assert.equal(await initialPosition, '58,56');
const retryPosition = getCurrentPosition();
assert.equal(requests, 2, 'a retry must request a fresh position');
finishPosition({ coords: { latitude: 59, longitude: 57 } });
assert.equal(await retryPosition, '59,57');
