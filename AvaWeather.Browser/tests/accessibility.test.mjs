// @ts-nocheck
// This Node test uses a deliberately small DOM fake that does not implement browser DOM types.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { renderForecast } from '../wwwroot/accessibility.js';

class FakeElement {
    /** @param {string} [tagName] */
    constructor(tagName = 'div') {
        this.tagName = tagName.toUpperCase();
        /** @type {FakeElement[]} */
        this.children = [];
        /** @type {Record<string, string>} */
        this.attributes = {};
        this.textContent = '';
        this.hidden = false;
        /** @type {Record<string, () => void>} */
        this.listeners = {};
    }
    /** @param {...FakeElement} children */
    append(...children) { this.children.push(...children); }
    /** @param {...FakeElement} children */
    replaceChildren(...children) { this.children = children; }
    /** @param {string} name @param {string} value */
    setAttribute(name, value) { this.attributes[name] = value; }
    /** @param {string} name @param {() => void} handler */
    addEventListener(name, handler) { this.listeners[name] = handler; }
    /** @param {string} tag @returns {FakeElement[]} */
    queryAll(tag) {
        return this.children.flatMap(child => [
            ...(child.tagName === tag.toUpperCase() ? [child] : []),
            ...child.queryAll(tag)
        ]);
    }
}

/** @type {Record<string, FakeElement>} */
const elements = {
    'forecast-link': new FakeElement(),
    'visual-link': new FakeElement(),
    'forecast-announcement': new FakeElement(),
    'accessible-forecast': new FakeElement(),
    'forecast-content': new FakeElement()
};
/** @param {string} id */
function byId(id) {
    const element = elements[id];
    if (!element) throw new Error(`Missing test element: ${id}`);
    return element;
}
const mockDocument = {
    documentElement: new FakeElement('html'),
    /** @param {string} id */
    getElementById: id => elements[id] ?? null,
    /** @param {string} tag */
    createElement: tag => new FakeElement(tag)
};
Object.defineProperty(globalThis, 'document', { configurable: true, value: mockDocument });
Object.defineProperty(globalThis, 'location', {
    configurable: true,
    value: { reload() { throw new Error('Retry called'); } }
});

const html = readFileSync(new URL('../wwwroot/index.html', import.meta.url), 'utf8');
const css = readFileSync(new URL('../wwwroot/app.css', import.meta.url), 'utf8');
assert.match(html, /id="forecast-link"[^>]*href="#accessible-forecast"/);
assert.match(html, /id="accessible-forecast"[^>]*tabindex="-1"/);
assert.match(html, /id="visual-link"[^>]*href="#out"/);
assert.match(css, /\.accessible-forecast:target/);
assert.match(css, /\.accessible-forecast:focus-within/);

renderForecast(JSON.stringify({
    Language: 'ru', LinkLabel: 'Текстовый прогноз', BackLabel: 'Вернуться',
    Heading: 'Пермь, Россия', Status: '', RetryLabel: 'Повторить',
    CurrentHeading: 'Текущая погода', Temperature: '10°C', Condition: 'Облачно',
    UpdatedLabel: 'Обновлено', Updated: '12:00',
    CurrentMetrics: [{ Label: 'Влажность', Value: '66%' }],
    HourlyHeading: 'Почасовой прогноз', Hourly: [{ Time: '13:00', Now: '', Temperature: '11°C', Condition: 'Дождь', Rain: '40%' }],
    DailyHeading: 'Прогноз на 3 дня', HighLabel: 'Максимум', LowLabel: 'Минимум',
    HumidityLabel: 'Влажность', WindLabel: 'Ветер', RainLabel: 'Осадки', UvLabel: 'УФ', SunriseSunsetLabel: 'Восход / Закат',
    Daily: [{ Name: 'Сегодня', Date: '15 окт. 2026', Maximum: '12°C', Minimum: '5°C',
        Condition: 'Облачно', Humidity: '66%', Wind: '8 км/ч', Rain: '40%', Uv: '1', SunriseSunset: '07:00 / 18:00' }]
}));

assert.equal(mockDocument.documentElement.attributes.lang, 'ru');
assert.equal(byId('forecast-link').textContent, 'Текстовый прогноз');
assert.equal(byId('forecast-link').hidden, false);
const content = byId('forecast-content');
assert.equal(content.queryAll('h1').length, 1);
assert.equal(content.queryAll('h2').length, 3);
assert.equal(content.queryAll('ol').length, 2);
assert.equal(content.queryAll('li').length, 2);
assert.equal(content.queryAll('dl').length, 2);

renderForecast(JSON.stringify({
    Language: 'en', LinkLabel: 'Text forecast', BackLabel: 'Back', Heading: 'Location unavailable',
    Status: 'No connection', RetryLabel: 'Retry',
    CurrentMetrics: [], Hourly: [], Daily: []
}));
assert.equal(byId('forecast-announcement').textContent, 'No connection');
assert.equal(byId('forecast-content').queryAll('button').length, 1);
assert.equal(byId('forecast-content').queryAll('a').length, 0);
assert.equal(byId('forecast-link').textContent, 'Text forecast');
