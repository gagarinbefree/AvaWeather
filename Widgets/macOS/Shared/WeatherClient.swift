import Foundation

struct WeatherClient {
    private let session: URLSession = {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = 7
        return URLSession(configuration: configuration)
    }()

    func load() async throws -> WeatherSnapshot {
        let location = try? await locate()
        return try await weatherApi(location)
    }

    func load(for location: Location) async throws -> WeatherSnapshot {
        try await weatherApi(location)
    }

    func locate() async throws -> Location {
        let data = try await fetch("https://get.geojs.io/v1/ip/geo.json")
        let json = try object(data)
        guard let city = json["city"] as? String, !city.isEmpty,
              let country = json["country"] as? String, !country.isEmpty,
              let lat = number(json["latitude"]), let lon = number(json["longitude"]),
              (-90...90).contains(lat), (-180...180).contains(lon), lat != 0 || lon != 0
        else { throw WeatherError.invalidResponse }
        return Location(city: city, country: country, latitude: lat, longitude: lon)
    }

    private func weatherApi(_ location: Location?) async throws -> WeatherSnapshot {
        guard let key = ProcessInfo.processInfo.environment["WEATHER_API_KEY"] ?? EmbeddedKey.read(),
              !key.isEmpty else { throw WeatherError.noFallbackKey }
        var components = URLComponents(string: "https://api.weatherapi.com/v1/current.json")!
        components.queryItems = [URLQueryItem(name: "key", value: key),
                                 URLQueryItem(name: "q", value: location.map { "\($0.latitude),\($0.longitude)" } ?? "auto:ip"),
                                 URLQueryItem(name: "lang", value: WidgetLocalization.isRussianCountry(location?.country ?? "") ? "ru" : "en")]
        var json = try object(await fetch(components.url!.absoluteString))
        if location == nil,
           let firstPlace = json["location"] as? [String: Any],
           let firstCountry = firstPlace["country"] as? String,
           WidgetLocalization.isRussianCountry(firstCountry) {
            components.queryItems?.removeAll { $0.name == "lang" }
            components.queryItems?.append(URLQueryItem(name: "lang", value: "ru"))
            json = try object(await fetch(components.url!.absoluteString))
        }
        guard let place = json["location"] as? [String: Any],
              let current = json["current"] as? [String: Any],
              let city = place["name"] as? String,
              let country = place["country"] as? String,
              let temp = number(current["temp_c"]),
              let feels = number(current["feelslike_c"]),
              let humidity = number(current["humidity"]),
              let day = number(current["is_day"]),
              let condition = current["condition"] as? [String: Any],
              let code = number(condition["code"]),
              let text = condition["text"] as? String
        else { throw WeatherError.invalidResponse }
        let resolvedCity = WidgetLocalization.isRussianCountry(country)
            ? await russianCity(Location(city: city, country: country,
                latitude: number(place["lat"]) ?? 0, longitude: number(place["lon"]) ?? 0)) ?? city
            : city
        return WeatherSnapshot.make(city: resolvedCity, country: country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity), condition: text,
            code: Int(code), isDay: day == 1)
    }

    private func russianCity(_ location: Location) async -> String? {
        guard let name = "\(location.city),\(location.country)".addingPercentEncoding(withAllowedCharacters: .urlQueryAllowed),
              let json = try? object(await fetch("https://geocoding-api.open-meteo.com/v1/search?name=\(name)&count=10&language=ru")),
              let results = json["results"] as? [[String: Any]] else { return nil }
        return results.compactMap { item -> (String, Double)? in
            guard let city = item["name"] as? String,
                  city.unicodeScalars.contains(where: { (0x0400...0x04FF).contains($0.value) }),
                  let lat = number(item["latitude"]), let lon = number(item["longitude"]) else { return nil }
            let distance = abs(lat - location.latitude) + abs(lon - location.longitude)
            return (city, distance)
        }.filter { $0.1 < 1 }.min { $0.1 < $1.1 }?.0
    }

    private func fetch(_ url: String) async throws -> Data {
        guard let address = URL(string: url) else { throw WeatherError.invalidResponse }
        let (data, response) = try await session.data(from: address)
        guard (response as? HTTPURLResponse)?.statusCode == 200 else { throw WeatherError.invalidResponse }
        return data
    }

    private func object(_ data: Data) throws -> [String: Any] {
        guard let result = try JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            throw WeatherError.invalidResponse
        }
        return result
    }

    private func number(_ value: Any?) -> Double? {
        if let number = value as? NSNumber { return number.doubleValue }
        if let text = value as? String { return Double(text) }
        return nil
    }

}

struct Location {
    let city: String
    let country: String
    let latitude: Double
    let longitude: Double

    func isSamePlace(as other: Location?) -> Bool {
        guard let other else { return false }
        return city.caseInsensitiveCompare(other.city) == .orderedSame &&
            country.caseInsensitiveCompare(other.country) == .orderedSame &&
            abs(latitude - other.latitude) < 0.1 && abs(longitude - other.longitude) < 0.1
    }
}

private enum WeatherError: LocalizedError {
    case invalidResponse
    case noFallbackKey

    var errorDescription: String? {
        switch self {
        case .invalidResponse: return WidgetLocalization.current.text("WidgetInvalidResponse")
        case .noFallbackKey: return WidgetLocalization.current.text("WidgetNoFallbackKey")
        }
    }
}
