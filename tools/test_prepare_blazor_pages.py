import json
import unittest
from pathlib import Path

from prepare_blazor_pages import prepare


class PrepareBlazorPagesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        repository = Path(__file__).resolve().parents[1]
        cls.temp_root = repository / "BlzrWeather" / "obj" / "pages-test"
        if not cls.temp_root.resolve().is_relative_to(repository):
            raise RuntimeError("Test temporary directory is outside the repository")
        cls.temp_root.mkdir(parents=True, exist_ok=True)

    def test_rewrites_base_path_and_injects_key_only_into_publish_folder(self):
        root = self.temp_root / "inject"
        root.mkdir(exist_ok=True)
        (root / "index.html").write_text('<base href="/" />', encoding="utf-8")
        (root / "appsettings.json").write_text(
            json.dumps({"WeatherApi": {"ApiKey": ""}}), encoding="utf-8"
        )

        prepare(root, "test-key")

        self.assertIn('<base href="/AvaWeather/" />',
                      (root / "index.html").read_text(encoding="utf-8"))
        self.assertEqual("test-key",
                         json.loads((root / "appsettings.json").read_text(
                             encoding="utf-8"))["WeatherApi"]["ApiKey"])
        self.assertTrue((root / ".nojekyll").exists())

    def test_refuses_to_publish_without_key(self):
        with self.assertRaises(ValueError):
            prepare(self.temp_root, "")


if __name__ == "__main__":
    unittest.main()
