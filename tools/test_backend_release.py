"""Guard release artifacts against embedded weather provider credentials."""

import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class BackendReleaseTests(unittest.TestCase):
    def test_release_workflow_never_receives_weather_provider_secrets(self):
        workflow = (ROOT / ".github/workflows/dotnet-tests.yml").read_text(encoding="utf-8")
        self.assertNotIn("WEATHER_API_KEY", workflow)
        self.assertNotIn("OPENWEATHERMAP_API_KEY", workflow)
        self.assertNotIn("EmbeddedKeySource", workflow)
        self.assertNotIn("embed_swift_key", workflow)
        self.assertIn("HAWK_DSN", workflow)

    def test_shared_configuration_has_only_a_public_https_backend(self):
        data = json.loads((ROOT / "SharedAssets/Configuration/weather-server.json").read_text(encoding="utf-8"))
        self.assertEqual({"baseUrl": "https://gagarinbefree.clodocd.com/"}, data)

    def test_runtime_clients_never_contain_direct_weather_provider_urls(self):
        forbidden = ("https://api.weatherapi.com/", "https://api.open-meteo.com/", "https://api.openweathermap.org/")
        for folder in ("Infrastructure", "AvaWeather", "AvaWeather.Android", "AvaWeather.Browser", "Widgets"):
            for source in (ROOT / folder).rglob("*"):
                if source.suffix not in (".cs", ".swift", ".json") or any(part in ("bin", "obj", "Properties") for part in source.parts):
                    continue
                if source.name.endswith(".generated.swift"):
                    continue
                text = source.read_text(encoding="utf-8")
                for address in forbidden:
                    self.assertNotIn(address, text, str(source.relative_to(ROOT)))
