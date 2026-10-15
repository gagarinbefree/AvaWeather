"""Check that the source and published browser site can be installed as a PWA."""

import json
import struct
import unittest
from pathlib import Path
from urllib.parse import urljoin


ROOT = Path(__file__).resolve().parents[1]
WEB_ROOT = ROOT / "AvaWeather.Browser" / "wwwroot"


class BrowserPwaTests(unittest.TestCase):
    def test_manifest_is_installable_under_the_pages_subpath(self):
        manifest = json.loads((WEB_ROOT / "manifest.webmanifest").read_text(encoding="utf-8"))
        self.assertEqual("AvaWeather", manifest["name"])
        self.assertEqual("./", manifest["start_url"])
        self.assertEqual("./", manifest["scope"])
        self.assertEqual("./", manifest["id"])
        self.assertEqual("standalone", manifest["display"])
        self.assertEqual("https://gagarinbefree.github.io/AvaWeather/",
                         urljoin("https://gagarinbefree.github.io/AvaWeather/manifest.webmanifest",
                                 manifest["start_url"]))
        self.assertEqual({"192x192", "512x512"},
                         {icon["sizes"] for icon in manifest["icons"] if icon["purpose"] == "any"})
        for icon in manifest["icons"]:
            image = ROOT / "SharedAssets" / "Branding" / Path(icon["src"]).name
            self.assertTrue(image.is_file(), image)
            data = image.read_bytes()
            self.assertEqual(b"\x89PNG\r\n\x1a\n", data[:8])
            width, height = struct.unpack(">II", data[16:24])
            expected = int(icon["sizes"].split("x")[0])
            self.assertEqual((expected, expected), (width, height))

    def test_home_page_links_manifest_and_has_offline_document(self):
        html = (WEB_ROOT / "index.html").read_text(encoding="utf-8")
        self.assertIn('rel="manifest" href="manifest.webmanifest"', html)
        self.assertIn('name="theme-color"', html)
        self.assertTrue((WEB_ROOT / "offline.html").is_file())


if __name__ == "__main__":
    unittest.main()
