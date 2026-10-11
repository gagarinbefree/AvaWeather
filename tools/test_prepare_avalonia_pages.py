import json
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
        for path in self.root.iterdir():
            path.unlink()
        self.root.rmdir()

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
