/** @typedef {{ Label: string, Value: string }} ForecastMetric */
/** @typedef {{ Time: string, Now: string, Temperature: string, Condition: string, Rain: string }} ForecastHour */
/** @typedef {{ Name: string, Date: string, Maximum: string, Minimum: string, Condition: string, Humidity: string, Wind: string, Rain: string, Uv: string, SunriseSunset: string }} ForecastDay */
/**
 * @typedef {object} ForecastData
 * @property {string} Language
 * @property {string} LinkLabel
 * @property {string} BackLabel
 * @property {string} Heading
 * @property {string} Status
 * @property {boolean} IsLoading
 * @property {string} RetryLabel
 * @property {string} CurrentHeading
 * @property {string} Temperature
 * @property {string} Condition
 * @property {string} UpdatedLabel
 * @property {string} Updated
 * @property {ForecastMetric[]} CurrentMetrics
 * @property {string} HourlyHeading
 * @property {ForecastHour[]} Hourly
 * @property {string} DailyHeading
 * @property {string} HighLabel
 * @property {string} LowLabel
 * @property {string} HumidityLabel
 * @property {string} WindLabel
 * @property {string} RainLabel
 * @property {string} UvLabel
 * @property {string} SunriseSunsetLabel
 * @property {ForecastDay[]} Daily
 */

/** @param {string} tag @param {string} [text] */
function element(tag, text) {
    const node = document.createElement(tag);
    if (text !== undefined) node.textContent = text;
    return node;
}

/** @param {string[][]} items */
function details(items) {
    const list = element('dl');
    for (const [label, value] of items) {
        list.append(element('dt', label), element('dd', value));
    }
    return list;
}

/** @param {string} id */
function requiredElement(id) {
    const node = document.getElementById(id);
    if (!node) throw new Error(id);
    return node;
}

/** @param {string} json */
export function renderForecast(json) {
    const forecast = /** @type {ForecastData} */ (JSON.parse(json));
    const link = requiredElement('forecast-link');
    const back = requiredElement('visual-link');
    const region = requiredElement('accessible-forecast');
    const content = requiredElement('forecast-content');
    const announcement = requiredElement('forecast-announcement');

    document.documentElement.setAttribute('lang', forecast.Language);
    link.textContent = forecast.LinkLabel;
    link.hidden = false;
    back.textContent = forecast.BackLabel;
    region.setAttribute('aria-busy', String(Boolean(forecast.IsLoading)));
    announcement.textContent = forecast.Status || forecast.Heading;

    const title = element('h1', forecast.Heading);
    title.id = 'accessible-forecast-heading';
    if (!forecast.Temperature) {
        const status = element('p', forecast.Status);
        const children = [title, status];
        if (forecast.Status && !forecast.IsLoading) {
            const retry = element('button', forecast.RetryLabel);
            retry.setAttribute('type', 'button');
            retry.addEventListener('click', () => location.reload());
            children.push(retry);
        }
        content.replaceChildren(...children);
        return;
    }

    const current = element('section');
    current.append(element('h2', forecast.CurrentHeading),
        element('p', `${forecast.Temperature} — ${forecast.Condition}`),
        details([
            [forecast.UpdatedLabel, forecast.Updated],
            ...forecast.CurrentMetrics.map(metric => [metric.Label, metric.Value])
        ]));

    const hourly = element('section');
    const hours = element('ol');
    for (const hour of forecast.Hourly) {
        const item = element('li');
        item.append(element('span', `${hour.Now ? `${hour.Now}, ` : ''}${hour.Time}: ${hour.Temperature}, ${hour.Condition}`));
        if (hour.Rain) item.append(element('span', ` — ${forecast.RainLabel}: ${hour.Rain}`));
        hours.append(item);
    }
    hourly.append(element('h2', forecast.HourlyHeading), hours);

    const daily = element('section');
    const days = element('ol');
    for (const day of forecast.Daily) {
        const item = element('li');
        item.append(element('h3', `${day.Name}, ${day.Date}`), element('p', day.Condition), details([
            [forecast.HighLabel, day.Maximum],
            [forecast.LowLabel, day.Minimum],
            [forecast.HumidityLabel, day.Humidity],
            [forecast.WindLabel, day.Wind],
            [forecast.RainLabel, day.Rain],
            [forecast.UvLabel, day.Uv],
            [forecast.SunriseSunsetLabel, day.SunriseSunset]
        ]));
        days.append(item);
    }
    daily.append(element('h2', forecast.DailyHeading), days);
    content.replaceChildren(title, current, hourly, daily);
}
