import SwiftUI
import WidgetKit

private struct WeatherEntry: TimelineEntry {
    let date: Date
    let snapshot: WeatherSnapshot
}

private actor WeatherStore {
    static let shared = WeatherStore()
    private var location: Location?
    private var snapshot: WeatherSnapshot?
    private var lastWeatherRefresh: Date?

    func refresh() async -> WeatherSnapshot {
        let client = WeatherClient()
        do {
            let detected = try await client.locate()
            if detected.isSamePlace(as: location), let snapshot,
               let lastWeatherRefresh, Date().timeIntervalSince(lastWeatherRefresh) < 20 * 60 {
                return snapshot
            }
            let updated = try await client.load(for: detected)
            location = detected
            snapshot = updated
            lastWeatherRefresh = .now
            return updated
        } catch {
            if let snapshot { return snapshot }
            if let updated = try? await client.load() {
                snapshot = updated
                lastWeatherRefresh = .now
                return updated
            }
            return .unavailable
        }
    }
}

private struct WeatherProvider: TimelineProvider {
    func placeholder(in context: Context) -> WeatherEntry {
        WeatherEntry(date: .now, snapshot: .placeholder)
    }

    func getSnapshot(in context: Context, completion: @escaping (WeatherEntry) -> Void) {
        if context.isPreview {
            completion(placeholder(in: context))
            return
        }
        Task {
            let weather = await WeatherStore.shared.refresh()
            completion(WeatherEntry(date: .now, snapshot: weather))
        }
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<WeatherEntry>) -> Void) {
        Task {
            let weather = await WeatherStore.shared.refresh()
            let now = Date()
            completion(Timeline(entries: [WeatherEntry(date: now, snapshot: weather)],
                                policy: .after(now.addingTimeInterval(30))))
        }
    }
}

private struct WeatherWidgetView: View {
    @Environment(\.widgetFamily) private var family
    let entry: WeatherEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 5) {
            HStack(alignment: .top) {
                Text(entry.snapshot.city)
                    .font(.headline)
                    .lineLimit(1)
                Spacer(minLength: 4)
                Image(systemName: entry.snapshot.symbol)
                    .font(.title2)
                    .accessibilityLabel(entry.snapshot.condition)
            }
            Spacer(minLength: 1)
            Text(entry.snapshot.temperature).font(.system(size: 43, weight: .semibold))
            Text(entry.snapshot.condition).font(.subheadline).lineLimit(1)
            if family != .systemSmall {
                Text(entry.snapshot.feelsLike).font(.caption)
                Text(entry.snapshot.humidity).font(.caption)
            }
        }
        .foregroundStyle(entry.snapshot.foreground)
        .padding(14)
        .containerBackground(entry.snapshot.background, for: .widget)
    }
}

@main
struct AvaWeatherWidget: Widget {
    let kind = "AvaWeather.Current"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: WeatherProvider()) { entry in
            WeatherWidgetView(entry: entry)
        }
        .configurationDisplayName("AvaWeather")
        .description(WidgetLocalization.current.text("WidgetDescription"))
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}
