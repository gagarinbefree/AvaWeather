// @ts-nocheck
import assert from 'node:assert/strict';
import { getCurrentPosition } from '../wwwroot/device-location.js';

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
