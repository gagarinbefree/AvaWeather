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
        symbol: WidgetConditionCatalog.symbol(for: "ClearDay"),
        background: ThemeColorService.shared.color("Weather.ClearDay.Current"),
        foreground: ThemeColorService.shared.color("Weather.ClearDay.Accent"))

    static let unavailable = WeatherSnapshot(city: "AvaWeather", temperature: "--°",
        feelsLike: "", condition: WidgetLocalization.current.text("WidgetUnavailable"), humidity: "",
        galleryHint: WidgetLocalization.current.text("WidgetGalleryHint"),
        symbol: WidgetConditionCatalog.symbol(for: "Cloudy"),
        background: ThemeColorService.shared.color("Weather.Cloudy.Current"),
        foreground: ThemeColorService.shared.color("Weather.Cloudy.Accent"))

    static func make(city: String, country: String, temperature: Double, feelsLike: Double,
                     humidity: Int, condition: String, code: Int, isDay: Bool) -> WeatherSnapshot {
        let strings = WidgetLocalization.forCountry(country)
        let style = WidgetConditionCatalog.style(for: code, isDay: isDay)
        let theme = ThemeColorService.shared
        return WeatherSnapshot(city: city,
            temperature: "\(Int(temperature.rounded()))°",
            feelsLike: "\(strings.text("WidgetFeelsLike")) \(Int(feelsLike.rounded()))°",
            condition: condition.prefix(1).uppercased() + String(condition.dropFirst()),
            humidity: "\(strings.text("Humidity")) \(humidity)%",
            galleryHint: strings.text("WidgetGalleryHint"),
            symbol: style.symbol,
            background: theme.color("Weather.\(style.kind).Current"),
            foreground: theme.color("Weather.\(style.kind).Accent"))
    }
}
