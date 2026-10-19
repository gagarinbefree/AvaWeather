// @ts-nocheck
import assert from 'node:assert/strict';
import { readWeatherCache, writeWeatherCache } from '../wwwroot/weather-cache.js';

const values = new Map();
Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: {
    getItem(key) { return values.get(key) ?? null; },
    setItem(key, value) { values.set(key, value); }
} });
assert.equal(readWeatherCache(), null);
writeWeatherCache('first');
assert.equal(readWeatherCache(), 'first');
writeWeatherCache('second');
assert.equal(readWeatherCache(), 'second');
Object.defineProperty(globalThis, 'localStorage', { configurable: true, get() { throw new Error('blocked'); } });
assert.equal(readWeatherCache(), null);
assert.doesNotThrow(() => writeWeatherCache('ignored'));
