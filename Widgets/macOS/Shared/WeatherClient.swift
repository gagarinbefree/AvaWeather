import Foundation

struct WeatherClient {
    private let session: URLSession = {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = 7
        return URLSession(configuration: configuration)
    }()

    func load() async throws -> WeatherSnapshot {
        let location = try? await locate()
        if let location {
            do { return try await openMeteo(location) }
            catch { /* WeatherAPI remains available when Open-Meteo is unreachable. */ }
        }
        return try await weatherApi(location)
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
        let russian = isRussian(location.country)
        let city = russian ? await russianCity(location) ?? location.city : location.city
        return WeatherSnapshot.make(city: city, country: location.country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity),
            condition: description(Int(code), russian: russian), code: Int(code), isDay: day == 1)
    }

    private func weatherApi(_ location: Location?) async throws -> WeatherSnapshot {
        guard let key = ProcessInfo.processInfo.environment["WEATHER_API_KEY"] ?? EmbeddedKey.read(),
              !key.isEmpty else { throw WeatherError.noFallbackKey }
        var components = URLComponents(string: "https://api.weatherapi.com/v1/current.json")!
        components.queryItems = [URLQueryItem(name: "key", value: key),
                                 URLQueryItem(name: "q", value: "auto:ip"),
                                 URLQueryItem(name: "lang", value: isRussian(location?.country ?? "") ? "ru" : "en")]
        var json = try object(await fetch(components.url!.absoluteString))
        if location == nil,
           let firstPlace = json["location"] as? [String: Any],
           let firstCountry = firstPlace["country"] as? String,
           isRussian(firstCountry) {
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
        let resolvedCity = isRussian(country)
            ? await russianCity(Location(city: city, country: country,
                latitude: number(place["lat"]) ?? 0, longitude: number(place["lon"]) ?? 0)) ?? city
            : city
        return WeatherSnapshot.make(city: resolvedCity, country: country, temperature: temp,
            feelsLike: feels, humidity: Int(humidity), condition: text,
            code: weatherApiToWmo(Int(code)), isDay: day == 1)
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

    private func isRussian(_ country: String) -> Bool {
        ["Russia", "Russian Federation", "Ukraine", "Belarus", "Kazakhstan", "Kyrgyzstan",
         "Uzbekistan", "Tajikistan", "Turkmenistan", "Moldova", "Armenia", "Azerbaijan",
         "Georgia", "Estonia", "Latvia", "Lithuania"].contains(country)
    }

    private func description(_ code: Int, russian: Bool) -> String {
        switch code {
        case 0: return russian ? "Ясно" : "Clear"
        case 1, 2: return russian ? "Переменная облачность" : "Partly cloudy"
        case 3: return russian ? "Облачно" : "Cloudy"
        case 45, 48: return russian ? "Туман" : "Fog"
        case 51...67, 80...82: return russian ? "Дождь" : "Rain"
        case 71...77, 85, 86: return russian ? "Снег" : "Snow"
        case 95...99: return russian ? "Гроза" : "Thunderstorm"
        default: return russian ? "Переменная облачность" : "Partly cloudy"
        }
    }

    private func weatherApiToWmo(_ code: Int) -> Int {
        switch code {
        case 1000: return 0
        case 1003: return 2
        case 1006, 1009: return 3
        case 1030, 1135, 1147: return 45
        case 1066, 1069, 1072, 1114, 1117, 1204, 1207, 1210...1237, 1249...1264: return 71
        case 1087, 1273, 1276, 1279, 1282: return 95
        default: return 61
        }
    }
}

private struct Location {
    let city: String
    let country: String
    let latitude: Double
    let longitude: Double
}

private enum WeatherError: Error {
    case invalidResponse
    case noFallbackKey
}
