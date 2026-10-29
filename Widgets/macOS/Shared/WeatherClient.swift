import Foundation

struct WeatherClient {
    private let session: URLSession = {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = 40
        return URLSession(configuration: configuration)
    }()

    func load() async throws -> WeatherSnapshot {
        let location = try? await locate()
        var sources: [() async throws -> WeatherSnapshot] = [{ try await weatherApi(location) }]
        if let location {
            sources.append { try await openMeteo(location) }
            sources.append { try await openWeatherMap(location) }
        }
        var retained: WeatherSnapshot?
        var lastError: Error = WeatherError.invalidResponse
        for source in sources {
            do {
                let snapshot = try await source()
                guard let time = snapshot.serverCachedAt else { return snapshot }
                if retained == nil || time > retained!.serverCachedAt! { retained = snapshot }
            } catch {
                lastError = error
                if case WeatherError.rateLimited = error { break }
            }
        }
        if let retained { return retained }
        throw lastError
    }

    private func openWeatherMap(_ location: Location) async throws -> WeatherSnapshot {
        var components = URLComponents(url: try backendURL("api/openweathermap/data/2.5/weather"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "lat", value: String(location.latitude)),
            URLQueryItem(name: "lon", value: String(location.longitude)), URLQueryItem(name: "units", value: "metric"),
            URLQueryItem(name: "lang", value: WidgetLocalization.isRussianCountry(location.country) ? "ru" : "en")]
        let response = try await fetchWeather(components.url!)
        let json = response.json
        guard let main = json["main"] as? [String: Any], let temp = number(main["temp"]),
              let feels = number(main["feels_like"]), let humidity = number(main["humidity"]),
              let conditions = json["weather"] as? [[String: Any]], let condition = conditions.first,
              let code = number(condition["id"]), let text = condition["description"] as? String,
              let icon = condition["icon"] as? String else { throw WeatherError.invalidResponse }
        let city = WidgetLocalization.isRussianCountry(location.country) ? await russianCity(location) ?? location.city : location.city
        return WeatherSnapshot.make(city: city, country: location.country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity), condition: text, code: Int(code), isDay: icon.hasSuffix("d"), cachedAt: response.retainedAt)
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
        let url = try backendURL("api/open-meteo/v1/forecast").absoluteString + "?latitude=\(location.latitude)&longitude=\(location.longitude)&timezone=auto&current=temperature_2m,apparent_temperature,relative_humidity_2m,is_day,weather_code"
        guard let address = URL(string: url) else { throw WeatherError.invalidResponse }
        let response = try await fetchWeather(address)
        let json = response.json
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
            code: Int(code), isDay: day == 1, cachedAt: response.retainedAt)
    }

    private func weatherApi(_ location: Location?) async throws -> WeatherSnapshot {
        var components = URLComponents(url: try backendURL("api/weatherapi/v1/current.json"), resolvingAgainstBaseURL: false)!
        components.queryItems = [URLQueryItem(name: "q", value: location.map { "\($0.latitude),\($0.longitude)" } ?? "auto:ip"),
                                 URLQueryItem(name: "lang", value: WidgetLocalization.isRussianCountry(location?.country ?? "") ? "ru" : "en")]
        var response = try await fetchWeather(components.url!)
        var json = response.json
        if location == nil,
           let firstPlace = json["location"] as? [String: Any],
           let firstCountry = firstPlace["country"] as? String,
           WidgetLocalization.isRussianCountry(firstCountry) {
            components.queryItems?.removeAll { $0.name == "lang" }
            components.queryItems?.append(URLQueryItem(name: "lang", value: "ru"))
            response = try await fetchWeather(components.url!)
            json = response.json
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
            code: Int(code), isDay: day == 1, cachedAt: response.retainedAt)
    }

    /// backendURL. Read the shared public endpoint for a fixed provider route.
    /// - Parameter path: The supported relative provider resource.
    /// - Returns: The HTTPS backend resource without client credentials.
    private func backendURL(_ path: String) throws -> URL {
        guard let resource = Bundle.main.url(forResource: "weather-server", withExtension: "json"),
              let data = try? Data(contentsOf: resource),
              let json = try? object(data), let value = json["baseUrl"] as? String,
              let root = URL(string: value), root.scheme == "https", root.host != nil,
              root.user == nil, root.password == nil, root.query == nil, root.fragment == nil,
              root.path.isEmpty || root.path == "/",
              let address = URL(string: path, relativeTo: root)?.absoluteURL
        else { throw WeatherError.invalidResponse }
        return address
    }

    /// fetchWeather. Read native JSON and the original retained server date.
    /// - Parameter url: The credential free backend resource.
    /// - Returns: The provider object and its retained date when fresh weather is unavailable.
    private func fetchWeather(_ url: URL) async throws -> (json: [String: Any], retainedAt: Date?) {
        let (data, response) = try await session.data(from: url)
        guard let http = response as? HTTPURLResponse else { throw WeatherError.invalidResponse }
        if http.statusCode == 429 && http.value(forHTTPHeaderField: "X-Weather-Provider") == nil {
            throw WeatherError.rateLimited
        }
        guard http.statusCode == 200 else { throw WeatherError.invalidResponse }
        var retainedAt: Date?
        if http.value(forHTTPHeaderField: "X-Weather-Cache")?.lowercased() == "stale" {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            let value = http.value(forHTTPHeaderField: "X-Weather-Fetched-At") ?? ""
            retainedAt = formatter.date(from: value)
            if retainedAt == nil {
                formatter.formatOptions = [.withInternetDateTime]
                retainedAt = formatter.date(from: value)
            }
            guard let date = retainedAt, date.timeIntervalSince1970 > 0 else { throw WeatherError.invalidResponse }
        }
        return (try object(data), retainedAt)
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

    /// fetch. Keep location and place name lookups within their existing timeout.
    /// - Parameter url: The public location or place name endpoint.
    /// - Returns: The unmodified response body.
    private func fetch(_ url: String) async throws -> Data {
        guard let address = URL(string: url) else { throw WeatherError.invalidResponse }
        var request = URLRequest(url: address)
        request.timeoutInterval = 7
        let (data, response) = try await session.data(for: request)
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
    case rateLimited

    var errorDescription: String? {
        switch self {
        case .invalidResponse: return WidgetLocalization.current.text("WidgetInvalidResponse")
        case .rateLimited: return WidgetLocalization.current.text("WeatherBackendRateLimited")
        }
    }
}
