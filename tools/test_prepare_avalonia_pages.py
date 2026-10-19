import json
import shutil
import unittest
from pathlib import Path
from uuid import uuid4

from prepare_avalonia_pages import prepare


class PrepareAvaloniaPagesTests(unittest.TestCase):
    def setUp(self):
        self.temp_root = Path(__file__).resolve().parents[1] / "artifacts" / "page-tests"
        self.temp_root.mkdir(parents=True, exist_ok=True)
        self.root = self.temp_root / f"case-{uuid4().hex}"
        self.root.mkdir()

    def tearDown(self):
        if not self.root.resolve().is_relative_to(self.temp_root.resolve()):
            raise ValueError("Test directory is outside the expected workspace")
        shutil.rmtree(self.root)

    def test_publish_has_pages_base_key_and_uncompressed_changed_files(self):
        root = self.root
        (root / "index.html").write_text('<base href="/" />', encoding="utf-8")
        (root / "appsettings.json").write_text(
            json.dumps({"WeatherApi": {"ApiKey": ""}}), encoding="utf-8"
        )
        for name in ("index.html", "appsettings.json"):
            for suffix in (".br", ".gz"):
                (root / (name + suffix)).write_bytes(b"old")

        prepare(root, "test-key", "https://public@k1.hawk.so/0", "openweather-test-key")

        self.assertEqual('<base href="/AvaWeather/" />', (root / "index.html").read_text())
        settings = json.loads((root / "appsettings.json").read_text())
        self.assertEqual("test-key", settings["WeatherApi"]["ApiKey"])
        self.assertEqual("openweather-test-key", settings["OpenWeatherMap"]["ApiKey"])
        self.assertEqual("https://public@k1.hawk.so/0", settings["Telemetry"]["Dsn"])
        self.assertTrue((root / ".nojekyll").exists())
        for name in ("index.html", "appsettings.json"):
            for suffix in (".br", ".gz"):
                self.assertFalse((root / (name + suffix)).exists())

    def test_missing_key_is_rejected_before_site_changes(self):
        root = self.root
        (root / "index.html").write_text('<base href="/" />', encoding="utf-8")
        (root / "appsettings.json").write_text('{"WeatherApi": {"ApiKey": ""}}', encoding="utf-8")

        with self.assertRaises(ValueError):
            prepare(root, "  ")

        self.assertEqual('<base href="/" />', (root / "index.html").read_text())

    def test_native_wasm_is_preloaded_from_the_published_manifest(self):
        root = self.root
        (root / "index.html").write_text(
            '<html><head><base href="/" /></head><body></body></html>', encoding="utf-8"
        )
        (root / "appsettings.json").write_text(
            '{"WeatherApi": {"ApiKey": ""}}', encoding="utf-8"
        )
        framework = root / "_framework"
        framework.mkdir()
        (framework / "dotnet.js").write_text(
            '/*json-start*/{"resources":{"wasmNative":['
            '{"name":"dotnet.native.abc.wasm","hash":"sha256-test"}]}}/*json-end*/',
            encoding="utf-8",
        )
        (framework / "dotnet.native.abc.wasm").write_bytes(b"wasm")

        prepare(root, "test-key")

        index = (root / "index.html").read_text(encoding="utf-8")
        self.assertIn(
            '<link rel="preload" href="_framework/dotnet.native.abc.wasm" '
            'as="fetch" type="application/wasm" crossorigin '
            'integrity="sha256-test" />',
            index,
        )

    def test_offline_manifest_contains_current_app_assets_but_not_settings_or_old_assemblies(self):
        root = self.root
        (root / "index.html").write_text('<head><base href="/" /></head>', encoding="utf-8")
        (root / "appsettings.json").write_text('{"WeatherApi": {"ApiKey": ""}}', encoding="utf-8")
        (root / "main.js").write_text('app', encoding="utf-8")
        (root / "sw.js").write_text("__OFFLINE_VERSION__", encoding="utf-8")
        framework = root / "_framework"
        framework.mkdir()
        (framework / "dotnet.js").write_text(
            '/*json-start*/{"resources":{"wasmNative":[{"name":"runtime.wasm","hash":"sha256-test"}],'
            '"assembly":[{"name":"current.wasm"}],"satelliteResources":{"ru":[{"name":"ru.wasm"}]}}}/*json-end*/',
            encoding="utf-8")
        (framework / "runtime.wasm").write_bytes(b"runtime")
        (framework / "current.wasm").write_bytes(b"current")
        (framework / "old.wasm").write_bytes(b"old")
        (framework / "ru").mkdir()
        (framework / "ru" / "ru.wasm").write_bytes(b"ru")
        prepare(root, "secret")
        files = json.loads((root / "offline-assets.json").read_text())
        self.assertIn("index.html", files)
        self.assertIn("main.js", files)
        self.assertIn("_framework/current.wasm", files)
        self.assertIn("_framework/ru/ru.wasm", files)
        self.assertNotIn("_framework/old.wasm", files)
        self.assertNotIn("appsettings.json", files)
        self.assertNotIn("secret", (root / "offline-assets.json").read_text())
        self.assertNotIn("__OFFLINE_VERSION__", (root / "sw.js").read_text())
