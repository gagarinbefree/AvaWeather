[![.NET Tests](https://github.com/gagarinbefree/AvaWeather/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/gagarinbefree/AvaWeather/actions/workflows/dotnet-tests.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1.3-6B4EFF)](https://avaloniaui.net/)
[![MVVM Toolkit](https://img.shields.io/badge/CommunityToolkit.Mvvm-8.4.2-0078D4)](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

# AvaWeather

<img src="AvaWeather/Assets/app-icon.png" alt="Иконка AvaWeather" width="96" height="96">

Кроссплатформенное настольное погодное приложение на .NET 9 и Avalonia. Нативное приложение для Windows, Linux и macOS.

**Последняя версия:** <!-- release-version -->v1.0.15<!-- /release-version -->

## Скачать

| Платформа | Сборка |
|-----------|--------|
| Windows x64 | [AvaWeather-win-x64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-win-x64.exe) |
| Windows ARM64 | [AvaWeather-win-arm64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-win-arm64.exe) |
| Linux x64 | [AvaWeather-linux-x64.tar.gz](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-linux-x64.tar.gz) |
| Linux ARM64 | [AvaWeather-linux-arm64.tar.gz](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-linux-arm64.tar.gz) |
| macOS Intel | [AvaWeather-osx-x64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-osx-x64) |
| macOS Apple Silicon | [AvaWeather-osx-arm64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-osx-arm64) |

Для каждой платформы и архитектуры публикуется один файл для скачивания. Windows сборка, самодостаточный `.exe`; архив Linux содержит один самодостаточный исполняемый файл `AvaWeather`; macOS сборка скачивается непосредственно как самодостаточный исполняемый файл. .NET Runtime устанавливать не нужно. На Linux распакуйте архив и запустите `./AvaWeather`. На macOS после скачивания выполните `chmod +x AvaWeather-osx-arm64 && ./AvaWeather-osx-arm64` (для Intel замените `arm64` на `x64`). Встроенная иконка появляется в Dock при работе приложения. Finder может показывать стандартный значок файла: собственный значок в Finder требует пакета `.app`.

## О проекте

Приложение автоматически определяет приблизительную локацию по IP через WeatherAPI и показывает погоду для найденного города:

- **Текущая погода** - температура, ощущается как, влажность, ветер, УФ-индекс, давление и видимость.
- **Почасовой прогноз** - оставшиеся часы сегодняшнего дня и следующий день по времени города.
- **Прогноз на 3 дня** - максимальная и минимальная температура, осадки, влажность, ветер и время восхода и заката.

### Особенности

- ✅ MVVM и генераторы свойств и команд CommunityToolkit.Mvvm.
- ✅ Compiled bindings и C# разметка `Avalonia.Markup.Declarative`.
- ✅ View Locator и Dependency Injection.
- ✅ Перестроение карточек при изменении ширины окна.
- ✅ Обработка сетевой ошибки с кнопкой Retry.
- ✅ Тесты
- ✅ Готовые погодные и интерфейсные иконки Fluent UI без собственных SVG-путей и Unicode-пиктограмм.
- ✅ Собственная иконка приложения для окна, Windows `.exe` и Dock на macOS.
- ✅ Мягкие цвета карточек и иконок меняются по погоде каждого периода: солнце, облака, дождь, снег, туман, гроза и ночь.

## Технологии

| Технология | Версия |
|------------|--------|
| .NET | 9.0 |
| Avalonia | 12.1.3 |
| Avalonia.Markup.Declarative | 12.1.1 |
| CommunityToolkit.Mvvm | 8.4.2 |
| FluentIcons.Avalonia | 2.1.343 |
| MediatR | 14.2.0 |
| AutoMapper | 16.2.0 |

## Установка и запуск

Требуется [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) и ключ [WeatherAPI](https://www.weatherapi.com/). На Windows, macOS или Linux:

1. Скопируйте `AvaWeather/appsettings.example.json` в `AvaWeather/appsettings.json` и укажите `WeatherApi.ApiKey` либо установите переменную окружения `WEATHER_API_KEY`.
2. Запустите приложение:

```sh
dotnet run --project AvaWeather/AvaWeather.csproj
```

Локальный `appsettings.json` исключён из Git и не входит в публикуемый исполняемый файл. Значение `WeatherApi.DefaultLocation` по умолчанию — `auto:ip`, поэтому локация определяется автоматически по IP. Адрес API и число дней прогноза также настраиваются в этом файле. Для готового релиза ключ WeatherAPI встроен при сборке; при необходимости его можно переопределить переменной окружения `WEATHER_API_KEY`.

## Тесты

```sh
dotnet test AvaWeather.sln
```

GitHub Actions запускает тесты при каждом push и pull request в `master` или `main`, а также сохраняет TRX-отчёт. После успешных тестов основной ветки он собирает шесть автономных файлов, создаёт GitHub Release, увеличивает версию `1.0.N` и обновляет её в README. Ссылки выше всегда ведут к последнему релизу. Тесты проверяют загрузку, повтор после ошибки, защиту от параллельных запросов, фильтрацию часов по времени города и отрисовку карточек в широком и узком окне. Для сохранения снимков headless-интерфейса задайте `AVAWEATHER_SCREENSHOT` и `AVAWEATHER_NARROW_SCREENSHOT` с путями к PNG.

Для публикации в настройках репозитория GitHub → **Settings → Secrets and variables → Actions** нужен секрет `WEATHER_API_KEY`. Сборка шифрует ключ AES-GCM и встраивает зашифрованные байты в приложение. Ключ расшифровки тоже находится в файле, так как приложение работает автономно; это скрывает ключ от простого поиска строк, но не защищает его от извлечения. Для настоящей защиты ключ должен оставаться на сервере-посреднике.

## Устройство

- `Domain`, `Application`, `Infrastructure` - логика модели, запросов WeatherAPI и преобразования данных.
- `AvaWeather/ViewModels` - MVVM-модель экрана на генераторах CommunityToolkit.Mvvm.
- `AvaWeather/Views` - интерфейс Avalonia на C# и `Avalonia.Markup.Declarative`; связи с данными созданы через `CompiledBinding`.
- `ViewLocator` - явное соответствие модели экрана и представления, создаваемого контейнером DI.

Иконки взяты из [FluentIcons.Avalonia](https://github.com/davidxuang/FluentIcons) и набора [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) под лицензией MIT. Условия погоды отображаются иконками пакета независимо от доступности сети.

Для собственной автономной сборки используйте `dotnet publish AvaWeather/AvaWeather.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` (или другой RID из таблицы). Без секрета сборки укажите свой ключ через `WEATHER_API_KEY` либо локальный `appsettings.json`.

![Интерфейс AvaWeather: солнечная погода, дождливые часы и разноцветный прогноз](docs/preview-weather-colors.png)
