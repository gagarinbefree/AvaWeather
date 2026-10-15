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

        prepare(root, "test-key")

        self.assertEqual('<base href="/AvaWeather/" />', (root / "index.html").read_text())
        self.assertEqual("test-key", json.loads((root / "appsettings.json").read_text())["WeatherApi"]["ApiKey"])
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
