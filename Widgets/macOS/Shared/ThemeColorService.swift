import SwiftUI

struct ThemeColorService {
    private struct Document: Decodable {
        let colors: [String: String]
    }

    static let shared = ThemeColorService(name: "default")
    private let colors: [String: String]

    init(name: String) {
        guard let url = Bundle.main.url(forResource: "\(name)-theme", withExtension: "json"),
              let data = try? Data(contentsOf: url),
              let document = try? JSONDecoder().decode(Document.self, from: data) else {
            preconditionFailure(WidgetLocalization.current.text("ThemeResourceMissing")
                .replacingOccurrences(of: "{0}", with: name))
        }
        colors = document.colors
    }

    func hex(_ name: String) -> String {
        guard let value = colors[name] else {
            preconditionFailure(WidgetLocalization.current.text("ThemeColorMissing")
                .replacingOccurrences(of: "{0}", with: name))
        }
        return value
    }

    func color(_ name: String) -> Color {
        let value = UInt(hex(name).dropFirst(), radix: 16)!
        return Color(.sRGB, red: Double((value >> 16) & 0xFF) / 255,
                     green: Double((value >> 8) & 0xFF) / 255,
                     blue: Double(value & 0xFF) / 255, opacity: 1)
    }
}
