import CryptoKit
import Foundation

func message(_ key: String) -> String {
    let language = Locale.current.language.languageCode?.identifier == "ru" ? "ru" : "en"
    let path = URL(fileURLWithPath: CommandLine.arguments.last ?? "")
        .deletingLastPathComponent().deletingLastPathComponent()
        .appendingPathComponent("Resources/\(language).lproj/Localizable.strings").path
    return (NSDictionary(contentsOfFile: path) as? [String: String])?[key] ?? key
}

guard CommandLine.arguments.count == 2 else { fatalError(message("SwiftKeyOutputRequired")) }

guard let raw = ProcessInfo.processInfo.environment["WEATHER_API_KEY"], !raw.isEmpty else {
    try "enum EmbeddedKey { static func read() -> String? { nil } }\n"
        .write(toFile: CommandLine.arguments[1], atomically: true, encoding: .utf8)
    print(message("SwiftKeyStubGenerated"))
    exit(0)
}

let key = SymmetricKey(size: .bits256)
let box = try AES.GCM.seal(Data(raw.utf8), using: key)
let keyData = key.withUnsafeBytes { Data($0) }
let generated = """
import CryptoKit
import Foundation

enum EmbeddedKey {
    static func read() -> String? {
        guard let key = Data(base64Encoded: "\(keyData.base64EncodedString())"),
              let sealed = Data(base64Encoded: "\(box.combined!.base64EncodedString())"),
              let box = try? AES.GCM.SealedBox(combined: sealed),
              let plain = try? AES.GCM.open(box, using: SymmetricKey(data: key)) else { return nil }
        return String(data: plain, encoding: .utf8)
    }
}
"""
try generated.write(toFile: CommandLine.arguments[1], atomically: true, encoding: .utf8)
print(message("SwiftKeySourceGenerated"))
