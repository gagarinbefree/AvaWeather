import SwiftUI
import WidgetKit

@main
struct AvaWeatherWidgetApp: App {
    @State private var snapshot = WeatherSnapshot.placeholder

    var body: some Scene {
        WindowGroup {
            VStack(alignment: .leading, spacing: 10) {
                Image(systemName: snapshot.symbol)
                    .font(.system(size: 44))
                Text(snapshot.city).font(.title2.bold())
                Text(snapshot.temperature).font(.system(size: 52, weight: .bold))
                Text(snapshot.condition)
                Text(snapshot.feelsLike).font(.subheadline)
                Text(snapshot.humidity).font(.subheadline)
                Text(snapshot.galleryHint)
                    .font(.caption).padding(.top, 12)
            }
            .foregroundStyle(snapshot.foreground)
            .padding(24)
            .frame(minWidth: 300, minHeight: 290)
            .background(snapshot.background)
            .task {
                let client = WeatherClient()
                var lastLocation: Location?
                var lastWeatherRefresh: Date?
                while !Task.isCancelled {
                    do {
                        let location = try await client.locate()
                        let changed = !location.isSamePlace(as: lastLocation)
                        if changed || (lastWeatherRefresh.map { Date().timeIntervalSince($0) >= 20 * 60 } ?? true) {
                            let updated = try await client.load(for: location)
                            snapshot = updated
                            lastLocation = location
                            lastWeatherRefresh = .now
                            if changed { WidgetCenter.shared.reloadTimelines(ofKind: "AvaWeather.Current") }
                        }
                    } catch {
                        if lastLocation == nil, let updated = try? await client.load() {
                            snapshot = updated
                            lastWeatherRefresh = .now
                        }
                    }
                    try? await Task.sleep(nanoseconds: 30_000_000_000)
                }
            }
        }
    }
}
