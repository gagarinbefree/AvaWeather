import SwiftUI

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
                Text("Add AvaWeather from the macOS widget gallery")
                    .font(.caption).padding(.top, 12)
            }
            .foregroundStyle(snapshot.foreground)
            .padding(24)
            .frame(minWidth: 300, minHeight: 290)
            .background(snapshot.background)
            .task {
                if let updated = try? await WeatherClient().load() {
                    await MainActor.run { snapshot = updated }
                }
            }
        }
    }
}
