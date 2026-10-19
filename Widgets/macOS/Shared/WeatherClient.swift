import Foundation

struct WeatherClient {
    private let session: URLSession = {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = 7
        return URLSession(configuration: configuration)
    }()

    func load() async throws -> WeatherSnapshot {
        let location = try? await locate()
        do { return try await weatherApi(location) }
        catch {
            guard let location else { throw error }
            do { return try await openMeteo(location) }
            catch { return try await openWeatherMap(location) }
        }
    }

    private func openWeatherMap(_ location: Location) async throws -> WeatherSnapshot {
        guard let key = ProcessInfo.processInfo.environment["OPENWEATHERMAP_API_KEY"] ?? EmbeddedOpenWeatherKey.read(), !key.isEmpty else { throw WeatherError.noFallbackKey }
        var components = URLComponents(string: "https://api.openweathermap.org/data/2.5/weather")!
        components.queryItems = [URLQueryItem(name: "lat", value: String(location.latitude)),
            URLQueryItem(name: "lon", value: String(location.longitude)), URLQueryItem(name: "units", value: "metric"),
            URLQueryItem(name: "lang", value: WidgetLocalization.isRussianCountry(location.country) ? "ru" : "en"),
            URLQueryItem(name: "appid", value: key)]
        let json = try object(await fetch(components.url!.absoluteString))
        guard let main = json["main"] as? [String: Any], let temp = number(main["temp"]),
              let feels = number(main["feels_like"]), let humidity = number(main["humidity"]),
              let conditions = json["weather"] as? [[String: Any]], let condition = conditions.first,
              let code = number(condition["id"]), let text = condition["description"] as? String,
              let icon = condition["icon"] as? String else { throw WeatherError.invalidResponse }
        let city = WidgetLocalization.isRussianCountry(location.country) ? await russianCity(location) ?? location.city : location.city
        return WeatherSnapshot.make(city: city, country: location.country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity), condition: text, code: Int(code), isDay: icon.hasSuffix("d"))
    }

    private func locate() async throws -> Location {
        let data = try await fetch("https://get.geojs.io/v1/ip/geo.json")
        let json = try object(data)
        guard let city = json["city"] as? String, !city.isEmpty,
              let country = json["country"] as? String, !country.isEmpty,
              let lat = number(json["latitude"]), let lon = number(json["longitude"]),
              (-90...90).contains(lat), (-180...180).contains(lon), lat != 0 || lon != 0
        else { throw WeatherError.invalidResponse }
        return Location(city: city, country: country, latitude: lat, longitude: lon)
    }

    private func openMeteo(_ location: Location) async throws -> WeatherSnapshot {
        let url = "https://api.open-meteo.com/v1/forecast?latitude=\(location.latitude)&longitude=\(location.longitude)&timezone=auto&current=temperature_2m,apparent_temperature,relative_humidity_2m,is_day,weather_code"
        let json = try object(await fetch(url))
        guard let current = json["current"] as? [String: Any],
              let temp = number(current["temperature_2m"]),
              let feels = number(current["apparent_temperature"]),
              let humidity = number(current["relative_humidity_2m"]),
              let code = number(current["weather_code"]),
              let day = number(current["is_day"])
        else { throw WeatherError.invalidResponse }
        let city = WidgetLocalization.isRussianCountry(location.country)
            ? await russianCity(location) ?? location.city : location.city
        return WeatherSnapshot.make(city: city, country: location.country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity),
            condition: WidgetLocalization.forCountry(location.country).condition(Int(code)),
            code: Int(code), isDay: day == 1)
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

private struct Location {
    let city: String
    let country: String
    let latitude: Double
    let longitude: Double
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
