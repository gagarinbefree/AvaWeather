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
        self._heading = False

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "a":
            self.links.append(attrs.get("href"))
        if tag == "meta":
            self.meta[attrs.get("name") or attrs.get("property")] = attrs.get("content")
        if tag == "link" and attrs.get("rel") == "canonical":
            self.canonical = attrs.get("href")
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
        page = self.parse_page("about.html")
        sitemap = ElementTree.parse(WEB_ROOT / "sitemap.xml")
        namespace = {"s": "http://www.sitemaps.org/schemas/sitemap/0.9"}
        locations = {
            element.text for element in sitemap.findall(".//s:loc", namespace)
        }

        self.assertEqual(SITE + "about.html", page.canonical)
        self.assertTrue(page.meta.get("description"))
        self.assertTrue(page.headings)
        self.assertIn(SITE, page.links)
        self.assertEqual({SITE, SITE + "about.html"}, locations)


if __name__ == "__main__":
    unittest.main()
