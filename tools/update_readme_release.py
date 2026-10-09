from pathlib import Path
import re
import sys


def main() -> None:
    if len(sys.argv) != 2 or not re.fullmatch(r"v\d+\.\d+\.\d+", sys.argv[1]):
        raise SystemExit("Usage: update_readme_release.py vMAJOR.MINOR.PATCH")

    readme = Path("README.md")
    content = readme.read_text(encoding="utf-8")
    updated, count = re.subn(
        r"(?<=<!-- release-version -->).*?(?=<!-- /release-version -->)",
        sys.argv[1],
        content,
    )
    if count != 1:
        raise SystemExit("README release version marker is missing or duplicated")
    readme.write_text(updated, encoding="utf-8")


if __name__ == "__main__":
    main()
