import SwiftUI

struct WeatherSnapshot {
    let city: String
    let temperature: String
    let feelsLike: String
    let condition: String
    let humidity: String
    let galleryHint: String
    let symbol: String
    let background: Color
    let foreground: Color

    static let placeholder = WeatherSnapshot(city: "AvaWeather", temperature: "24°",
        feelsLike: "\(WidgetLocalization.current.text("WidgetFeelsLike")) 22°", condition: WidgetLocalization.current.text("WidgetPlaceholderCondition"), humidity: "\(WidgetLocalization.current.text("Humidity")) 47%",
        galleryHint: WidgetLocalization.current.text("WidgetGalleryHint"),
        symbol: "sun.max.fill", background: Color(hex: 0xFFF0B8), foreground: Color(hex: 0x7C5700))

    static let unavailable = WeatherSnapshot(city: "AvaWeather", temperature: "--°",
        feelsLike: "", condition: WidgetLocalization.current.text("WidgetUnavailable"), humidity: "",
        galleryHint: WidgetLocalization.current.text("WidgetGalleryHint"),
        symbol: "cloud.fill", background: Color(hex: 0xDFE8EF), foreground: Color(hex: 0x435B70))

    static func make(city: String, country: String, temperature: Double, feelsLike: Double,
                     humidity: Int, condition: String, code: Int, isDay: Bool) -> WeatherSnapshot {
        let strings = WidgetLocalization.forCountry(country)
        let palette: (UInt, UInt, String)
        switch code {
        case 0 where !isDay: palette = (0xDCE5F5, 0x344E78, "moon.stars.fill")
        case 0: palette = (0xFFF0B8, 0x7C5700, "sun.max.fill")
        case 1, 2: palette = isDay
            ? (0xF4EBD4, 0x6D5A2F, "cloud.sun.fill")
            : (0xDCE5F5, 0x344E78, "cloud.moon.fill")
        case 3: palette = (0xDFE8EF, 0x435B70, "cloud.fill")
        case 45, 48: palette = (0xE7EAE5, 0x52645B, "cloud.fog.fill")
        case 51...67, 80...82: palette = (0xD7EAF5, 0x255B78, "cloud.rain.fill")
        case 71...77, 85, 86: palette = (0xE7EEF8, 0x496382, "cloud.snow.fill")
        case 95...99: palette = (0xE4DFF2, 0x5A4F78, "cloud.bolt.rain.fill")
        default: palette = (0xF4EBD4, 0x6D5A2F, "cloud.sun.fill")
        }
        return WeatherSnapshot(city: city,
            temperature: "\(Int(temperature.rounded()))°",
            feelsLike: "\(strings.text("WidgetFeelsLike")) \(Int(feelsLike.rounded()))°",
            condition: condition.prefix(1).uppercased() + String(condition.dropFirst()),
            humidity: "\(strings.text("Humidity")) \(humidity)%",
            galleryHint: strings.text("WidgetGalleryHint"),
            symbol: palette.2, background: Color(hex: palette.0), foreground: Color(hex: palette.1))
    }
}

private extension Color {
    init(hex: UInt) {
        self.init(.sRGB, red: Double((hex >> 16) & 0xFF) / 255,
                  green: Double((hex >> 8) & 0xFF) / 255,
                  blue: Double(hex & 0xFF) / 255, opacity: 1)
    }
}
