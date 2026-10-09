"""Verify that the release key can load both weather endpoints without printing it."""

import json
import os
import sys
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import urlopen


def check(endpoint: str, days: int | None = None) -> dict:
    params = {"key": os.environ["WEATHER_API_KEY"], "q": "58.0047,56.2514"}
    if days is not None:
        params["days"] = str(days)
    url = f"https://api.weatherapi.com/v1/{endpoint}.json?{urlencode(params)}"
    try:
        with urlopen(url, timeout=15) as response:
            return json.load(response)
    except HTTPError as error:
        raise RuntimeError(f"WeatherAPI {endpoint}: HTTP {error.code}") from None
    except URLError as error:
        raise RuntimeError(f"WeatherAPI {endpoint}: network error ({error.reason})") from None


def main() -> None:
    if not os.environ.get("WEATHER_API_KEY"):
        raise RuntimeError("WEATHER_API_KEY is empty")
    current = check("current")
    forecast = check("forecast", 3)
    if not current.get("current") or not current.get("location"):
        raise RuntimeError("WeatherAPI current: missing weather or location")
    days = forecast.get("forecast", {}).get("forecastday", [])
    if len(days) != 3 or not forecast.get("location"):
        raise RuntimeError("WeatherAPI forecast: expected three days and a location")
    print("WeatherAPI release key: current weather and three-day forecast are available")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, ValueError, json.JSONDecodeError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
