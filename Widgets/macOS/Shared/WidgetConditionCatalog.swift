import Foundation

struct WidgetConditionStyle {
    let kind: String
    let symbol: String
}

enum WidgetConditionCatalog {
    private struct Entry: Decodable {
        let kind: String
        let nightKind: String?
        let widgetSymbol: String
        let codes: [Int]
    }

    private static let entries: [Entry] = {
        guard let url = Bundle.main.url(forResource: "weather-conditions", withExtension: "json"),
              let data = try? Data(contentsOf: url),
              let entries = try? JSONDecoder().decode([Entry].self, from: data) else {
            return []
        }
        return entries
    }()

    static func style(for code: Int, isDay: Bool) -> WidgetConditionStyle {
        let entry = entries.first { $0.codes.contains(code) }
        let kind = !isDay ? entry?.nightKind ?? entry?.kind : entry?.kind
        let resolved = kind ?? "PartlyCloudy"
        return WidgetConditionStyle(kind: resolved, symbol: symbol(for: resolved))
    }

    static func symbol(for kind: String) -> String {
        entries.first { $0.kind == kind }?.widgetSymbol ?? "cloud.sun.fill"
    }
}
