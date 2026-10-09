import Foundation

struct WidgetLocalization {
    private let bundle: Bundle

    static let current = WidgetLocalization(language: Locale.current.language.languageCode?.identifier == "ru" ? "ru" : "en")

    static func forCountry(_ country: String) -> WidgetLocalization {
        WidgetLocalization(language: isRussianCountry(country) ? "ru" : "en")
    }

    static func isRussianCountry(_ country: String) -> Bool {
        let names = ["Russia", "Ukraine", "Belarus", "Kazakhstan", "Kyrgyzstan", "Uzbekistan",
                     "Tajikistan", "Turkmenistan", "Moldova", "Armenia", "Azerbaijan",
                     "Georgia", "Estonia", "Latvia", "Lithuania"]
        if country == "Russian Federation" || names.contains(country) { return true }
        let russian = WidgetLocalization(language: "ru")
        return names.contains { russian.text("Country\($0)") == country }
    }

    private init(language: String) {
        let path = Bundle.main.path(forResource: language, ofType: "lproj")
        bundle = path.flatMap(Bundle.init(path:)) ?? .main
    }

    func text(_ key: String) -> String {
        bundle.localizedString(forKey: key, value: nil, table: nil)
    }

}
