import unittest
from html.parser import HTMLParser
from pathlib import Path
from xml.etree import ElementTree


SITE = "https://gagarinbefree.github.io/AvaWeather/"
WEB_ROOT = Path(__file__).resolve().parents[1] / "AvaWeather.Browser" / "wwwroot"


class PageParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []
        self.meta = {}
        self.headings = []
        self.canonical = None
        self.lang = None
        self.alternates = {}
        self.images = []
        self._heading = False

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "html":
            self.lang = attrs.get("lang")
        if tag == "a":
            self.links.append(attrs.get("href"))
        if tag == "img":
            self.images.append(attrs)
        if tag == "meta":
            self.meta[attrs.get("name") or attrs.get("property")] = attrs.get("content")
        if tag == "link" and attrs.get("rel") == "canonical":
            self.canonical = attrs.get("href")
        if tag == "link" and attrs.get("rel") == "alternate":
            self.alternates[attrs.get("hreflang")] = attrs.get("href")
        self._heading = tag == "h1"

    def handle_endtag(self, tag):
        if tag == "h1":
            self._heading = False

    def handle_data(self, data):
        if self._heading and data.strip():
            self.headings.append(data.strip())


class BrowserSeoTests(unittest.TestCase):
    def parse_page(self, name):
        parser = PageParser()
        parser.feed((WEB_ROOT / name).read_text(encoding="utf-8"))
        return parser

    def test_home_page_has_crawlable_content_and_metadata(self):
        page = self.parse_page("index.html")

        self.assertEqual(SITE, page.canonical)
        self.assertTrue(page.meta.get("description"))
        self.assertEqual(SITE, page.meta.get("og:url"))
        self.assertIn("AvaWeather", page.headings)
        self.assertIn("about.html", page.links)

    def test_static_information_page_is_linked_and_in_sitemap(self):
        english = self.parse_page("about.html")
        russian = self.parse_page("ru/about.html")
        sitemap = ElementTree.parse(WEB_ROOT / "sitemap.xml")
        namespace = {"s": "http://www.sitemaps.org/schemas/sitemap/0.9"}
        locations = {
            element.text for element in sitemap.findall(".//s:loc", namespace)
        }

        expected_alternates = {"en": SITE + "about.html", "ru": SITE + "ru/about.html"}
        for page, lang, canonical in (
            (english, "en", SITE + "about.html"),
            (russian, "ru", SITE + "ru/about.html"),
        ):
            self.assertEqual(lang, page.lang)
            self.assertEqual(canonical, page.canonical)
            self.assertEqual(canonical, page.meta.get("og:url"))
            self.assertEqual(expected_alternates, page.alternates)
            self.assertTrue(page.meta.get("description"))
            self.assertTrue(page.headings)
            self.assertIn(SITE, page.links)
            self.assertTrue(any("/releases/latest" in link for link in page.links))
            self.assertTrue(any("preview-weather-colors.png" in image.get("src", "")
                                and image.get("alt") for image in page.images))

        self.assertIn("ru/about.html", english.links)
        self.assertIn("../about.html", russian.links)
        self.assertEqual({SITE, SITE + "about.html", SITE + "ru/about.html"}, locations)

    def test_search_console_verification_file_is_kept(self):
        name = "google4c6e30926229c0e5.html"
        self.assertEqual(
            "google-site-verification: " + name,
            (WEB_ROOT / name).read_text(encoding="utf-8").strip(),
        )


if __name__ == "__main__":
    unittest.main()
