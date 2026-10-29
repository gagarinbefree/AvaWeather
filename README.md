[![.NET Tests](https://github.com/gagarinbefree/AvaWeather/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/gagarinbefree/AvaWeather/actions/workflows/dotnet-tests.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1.3-6B4EFF)](https://avaloniaui.net/)
[![Avalonia Browser](https://img.shields.io/badge/Avalonia-Browser%20WASM-6B4EFF)](https://docs.avaloniaui.net/docs/deployment/webassembly)
[![MVVM Toolkit](https://img.shields.io/badge/CommunityToolkit.Mvvm-8.4.2-0078D4)](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
[![WeatherAPI](https://img.shields.io/badge/WeatherAPI-API%20v1-3D8EDB)](https://www.weatherapi.com/)
[![Open-Meteo](https://img.shields.io/badge/Open--Meteo-API%20v1-4C9C86)](https://open-meteo.com/)
[![OpenWeatherMap](https://img.shields.io/badge/OpenWeatherMap-API%20v2.5-orange)](https://openweathermap.org/)

# AvaWeather

<img src="SharedAssets/Branding/app-icon.png" alt="Иконка AvaWeather" width="96" height="96">

Кроссплатформенное погодное приложение на .NET 10 и Avalonia. Есть настольные версии для Windows, Linux и macOS, веб-версия и проект приложения для Android.

**Последняя версия:** <!-- release-version -->v1.0.100<!-- /release-version -->

**Веб-версия:** [Открыть AvaWeather](https://gagarinbefree.github.io/AvaWeather/). Сайт показывает ту же погоду и трёхдневный прогноз, автоматически определяя местоположение. Его можно установить как PWA через меню браузера. После успешной загрузки и сохранения файлов приложения при отсутствии сети можно открыть последний полученный прогноз с временем его получения. Для актуального прогноза нужен интернет. Сайт собирается из `AvaWeather.Browser` на Avalonia WebAssembly после тестов и публикуется в GitHub Pages.

Приложение и сайт сохраняют последний успешный прогноз на устройстве. При следующем запуске он показывается, пока загружается актуальная погода; если обновить данные не удалось, сохранённый прогноз остаётся на экране с сообщением об ошибке. На сайте прогноз хранится в хранилище браузера, а файлы приложения сохраняются service worker для офлайн-запуска. Очистка данных сайта удалит сохранённый прогноз и офлайн-файлы.

О приложении на сайте: [по-русски](https://gagarinbefree.github.io/AvaWeather/ru/about.html) · [in English](https://gagarinbefree.github.io/AvaWeather/about.html).

Инструкции по установке: [по-русски](https://gagarinbefree.github.io/AvaWeather/ru/install.html) · [in English](https://gagarinbefree.github.io/AvaWeather/install.html).

[Карта сайта для поисковых сервисов](https://gagarinbefree.github.io/AvaWeather/sitemap.xml).

### SEO и индексация сайта

Веб-приложение Avalonia рисует прогноз на canvas, поэтому для поисковых систем созданы отдельные HTML-страницы о приложении [на русском](https://gagarinbefree.github.io/AvaWeather/ru/about.html) и [на английском](https://gagarinbefree.github.io/AvaWeather/about.html), а также страницы установки для Windows, macOS и Linux на обоих языках. Страницы о приложении содержат описание возможностей и снимок интерфейса; страницы установки — инструкции и ссылки на актуальные сборки. Языковые версии связаны атрибутами `hreflang`, имеют собственные `canonical`-адреса и перечислены в `sitemap.xml`. Главная страница также содержит описание приложения до загрузки WebAssembly и прямую ссылку на русскую страницу. Персональный прогноз определяется по геолокации устройства или по IP при открытии сайта и не является отдельной индексируемой страницей города.

Сайт с адресом `https://gagarinbefree.github.io/AvaWeather/` подтверждён в Google Search Console как свойство **URL-prefix**. Файл [подтверждения владения](https://gagarinbefree.github.io/AvaWeather/google4c6e30926229c0e5.html) включён в публикацию GitHub Pages. [Карта сайта](https://gagarinbefree.github.io/AvaWeather/sitemap.xml) содержит главную и четыре информационные страницы.

## Скачать

| Платформа           | Автономный файл                                                                                                                     | Установщик                                                                                                                            |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Windows x64         | [AvaWeather-win-x64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-win-x64.exe)               | [AvaWeather-setup-win-x64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-setup-win-x64.exe)     |
| Windows ARM64       | [AvaWeather-win-arm64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-win-arm64.exe)           | [AvaWeather-setup-win-arm64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-setup-win-arm64.exe) |
| Linux x64           | [AvaWeather-linux-x64.tar.gz](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-linux-x64.tar.gz)     | —                                                                                                                                     |
| Linux ARM64         | [AvaWeather-linux-arm64.tar.gz](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-linux-arm64.tar.gz) | —                                                                                                                                     |
| macOS Intel         | [AvaWeather-osx-x64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-osx-x64)                       | [AvaWeather-setup-osx-x64.pkg](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-setup-osx-x64.pkg)     |
| macOS Apple Silicon | [AvaWeather-osx-arm64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-osx-arm64)                   | [AvaWeather-setup-osx-arm64.pkg](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-setup-osx-arm64.pkg) |
| Android             | [AvaWeather-android.apk](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-android.apk)               | —                                                                                                                                     |

Установщик Windows добавляет приложение в меню «Пуск», при желании создаёт ярлык на рабочем столе и регистрирует удаление в настройках Windows. Пакет macOS устанавливает `AvaWeather.app` с иконкой в `/Applications`. Оба установщика содержат то же автономное приложение, что доступно отдельным файлом. Установщики приложения пока не подписаны доверенными сертификатами; Windows и macOS могут запросить подтверждение запуска.

### Системные виджеты Windows 11 и macOS

| Платформа           | Установщик виджета                                                                                                                                  |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Windows 11 x64      | [AvaWeather-widget-setup-win-x64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-setup-win-x64.exe)     |
| Windows 11 ARM64    | [AvaWeather-widget-setup-win-arm64.exe](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-setup-win-arm64.exe) |
| macOS Intel         | [AvaWeather-widget-osx-x64.zip](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-osx-x64.zip)                 |
| macOS Apple Silicon | [AvaWeather-widget-osx-arm64.zip](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-osx-arm64.zip)             |

Виджеты собираются отдельно от автономного приложения: системные панели требуют пакет MSIX в Windows и расширение WidgetKit внутри `.app` в macOS. Linux-пакеты виджетов не выпускаются. Виджет обновляет погоду самостоятельно, когда окно приложения закрыто, выбирает язык по стране, определённой через IP, и получает погоду из WeatherAPI с резервными Open-Meteo и OpenWeatherMap.

Установщик Windows содержит `.msix` и публичный сертификат подписи внутри одного `.exe`. Он показывает отпечаток сертификата, проверяет целостность вложенных файлов и подпись пакета, затем запрашивает разрешение администратора, чтобы добавить сертификат в **Доверенные лица локального компьютера**, и устанавливает виджет для текущего пользователя. После этого добавьте AvaWeather в панели виджетов Windows. Сам установщик подписан тем же сертификатом для разработки; до первого доверия Windows может показать предупреждение о неизвестном издателе. Отдельные [MSIX x64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-win-x64.msix), [MSIX ARM64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-win-arm64.msix), [CER x64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-win-x64.cer) и [CER ARM64](https://github.com/gagarinbefree/AvaWeather/releases/latest/download/AvaWeather-widget-win-arm64.cer) доступны для ручной установки. Для неё сертификат нужно импортировать в хранилище **Доверенные лица локального компьютера** с правами администратора.

Сертификат для разработки создаётся заново для каждого релиза и остаётся в хранилище после удаления виджета; его можно удалить вручную по показанному отпечатку. Для распространения без такого доверия нужна подпись от доверенного издателя или публикация через Microsoft Store.

На macOS распакуйте `.zip`, перенесите `AvaWeatherWidgetHost.app` в Applications и запустите его один раз. Затем добавьте AvaWeather на рабочий стол или в Центр уведомлений через галерею виджетов. Пакет подписан локальной подписью без Apple Developer ID; macOS может попросить подтвердить первый запуск в настройках безопасности. Для распространения без этого шага понадобятся Apple Developer ID и notarization.

Автономные файлы остаются доступны для всех платформ. Windows сборка — самодостаточный `.exe`; архив Linux содержит один самодостаточный исполняемый файл `AvaWeather`; macOS сборка скачивается непосредственно как самодостаточный исполняемый файл. .NET Runtime устанавливать не нужно. На Linux распакуйте архив и запустите `./AvaWeather`. На macOS для отдельного файла выполните `chmod +x AvaWeather-osx-arm64 && ./AvaWeather-osx-arm64` (для Intel замените `arm64` на `x64`). Установщик macOS добавляет приложение с иконкой в Finder и Dock.

## О проекте

Android-приложение и сайт, в том числе установленный как PWA, запрашивают доступ к геолокации устройства и получают погоду для его координат из WeatherAPI. При отказе или недоступности геолокации используется приблизительная локация по IP через GeoJS. Настольное приложение и виджеты используют IP. Если GeoJS недоступен, WeatherAPI определяет местоположение по IP:

- **Текущая погода** - температура, ощущается как, влажность, ветер, УФ-индекс, давление и видимость.
- **Почасовой прогноз** - оставшиеся часы сегодняшнего дня и следующий день по времени города.
- **Прогноз на 3 дня** - максимальная и минимальная температура, осадки, влажность, ветер и время восхода и заката.

### Особенности

- ✅ MVVM и генераторы свойств и команд CommunityToolkit.Mvvm.
- ✅ Compiled bindings и C# разметка `Avalonia.Markup.Declarative`.
- ✅ View Locator и Dependency Injection.
- ✅ Автоматический выбор русского или английского языка по стране, определённой через IP; перевод подписей, ошибок, дат, единиц измерения и описаний погоды.
- ✅ Перестроение карточек при изменении ширины окна.
- ✅ Обработка сетевой ошибки с кнопками повторной попытки и отчёта о проблеме.
- ✅ Тесты
- ✅ Готовые погодные и интерфейсные иконки Fluent UI без собственных SVG-путей и Unicode-пиктограмм.
- ✅ Собственная иконка приложения для окна, Windows `.exe` и Dock на macOS.
- ✅ Мягкие цвета карточек и иконок меняются по погоде каждого периода: солнце, облака, дождь, снег, туман, гроза и ночь.

## Технологии

| Технология                  | Версия    |
| --------------------------- | --------- |
| .NET                        | 10.0      |
| Avalonia                    | 12.1.3    |
| Avalonia.Markup.Declarative | 12.1.1    |
| CommunityToolkit.Mvvm       | 8.4.2     |
| Svg.Controls.Skia.Avalonia  | 12.0.0.17 |
| MediatR                     | 14.2.0    |
| AutoMapper                  | 16.2.0    |

## Язык интерфейса

После определения страны приложение включает русский язык для России, Украины, Беларуси, Казахстана, Кыргызстана, Узбекистана, Таджикистана, Туркменистана, Молдовы, Армении, Азербайджана, Грузии, Эстонии, Латвии и Литвы. Для остальных стран или если страну не удалось определить используется английский. Выбор не зависит от языка операционной системы и не требует ручной настройки.

Подписи интерфейса и сообщения ошибок хранятся в `Localization/*.resx`; C# получает их через `StringLocalizer` по имени. Для macOS сборка экспортирует те же переводы в `.strings` через `tools/export_swift_localization.py`. WeatherAPI возвращает описания погодных условий на выбранном языке. Соответствие кодов погоды и иконок Fluent UI хранится в `SharedAssets/Weather/weather-conditions.json` и используется общим каталогом `Application.Weather.WeatherConditionCatalog`. Единственный исходный файл цветов интерфейса и погодных карточек — `SharedAssets/Themes/default.json`; общий `ThemeColorService` возвращает цвет по имени сайту, Avalonia-приложению и Windows-виджету. Из него же при сборке создаётся ресурс для виджета macOS. Для русского интерфейса приложение отдельно уточняет русское название города и региона по координатам. Если сервис названий недоступен, прогноз продолжает работать с исходным названием.

Запросы погоды проходят через наш [HTTPS-сервер](https://gagarinbefree.clodocd.com/health/live). Данные о погоде: [WeatherAPI](https://www.weatherapi.com/), при недоступности — [Open-Meteo](https://open-meteo.com/), затем [OpenWeatherMap](https://openweathermap.org/). Для резервного источника нужны координаты устройства или GeoJS; описания условий хранятся в ресурсах приложения. Геолокация: [GeoJS](https://www.geojs.io/docs/v1/endpoints/geo/); названия мест основаны на [GeoNames](https://www.geonames.org/) ([CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)).

## Диагностика ошибок

События обращений к сервисам хранятся в памяти и не создают файлы журнала в Release. Локальный файл `logs/api-access.log` создаётся только в Debug настольного и Android-приложения.

Для автоматической диагностики выбран российский [Хоук](https://hawk-tracker.ru/), который принимает Sentry-совместимые события. Создайте в Хоуке проект, скопируйте **Sentry DSN** из Project Settings → Integration и добавьте его как секрет GitHub Actions `HAWK_DSN`. Тогда при следующей сборке настольное приложение, Android и сайт/PWA будут отправлять событие при окончательной ошибке загрузки. Пока секрет не задан, автоматическая отправка выключена. Локально можно задать переменную окружения `HAWK_DSN` (значение `off` отключает отправку) или `Telemetry:Dsn` в настройках приложения. Для сайта DSN записывается в опубликованный `appsettings.json`, для настольного приложения и Android встраивается в сборку. DSN является публичным идентификатором проекта, поэтому будет виден пользователям. Событие содержит только тип сбоя, версию, платформу и до шести коротких записей из очищенного журнала; текст исключения, ключ WeatherAPI, IP, координаты, город и ответы API в него не включаются. При отправке Хоук видит IP источника соединения. Повтор одинакового сбоя ограничен одним сообщением за 10 минут на запуск приложения; недоступность Хоука не мешает повторить запрос погоды.

## Установка и запуск

Требуется [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Погодные ключи не нужны: приложение обращается к общему HTTPS-серверу. На Windows, macOS или Linux:

Запустите приложение:

```sh
dotnet run --project AvaWeather/AvaWeather.csproj
```

Локальный `appsettings.json` необязателен, исключён из Git и не входит в публикуемый исполняемый файл. Значение `WeatherApi.DefaultLocation` по умолчанию — `auto:ip`; при недоступности геолокации и GeoJS сервер использует IP клиента, а не местоположение VPS. Общий публичный адрес сервера хранится в `SharedAssets/Configuration/weather-server.json` и используется всеми платформами и виджетами.

Для локального запуска сайта установите .NET 10 SDK и `dotnet workload install wasm-tools`, затем выполните `dotnet run --project AvaWeather.Browser/AvaWeather.Browser.csproj`. Файл с погодными ключами не требуется. Необязательный `appsettings.Development.json` можно использовать для локальной настройки телеметрии; он исключён из Git и публикации.

### Android

Установите .NET 10 SDK, Android SDK, JDK 17 и нагрузку `dotnet workload install android`. Для запуска через F5 в Visual Studio установите рабочий набор **Разработка пользовательского интерфейса многоплатформенных приложений .NET** и отдельный компонент **Android SDK setup** во вкладке **Отдельные компоненты**. `.vsconfig` в корне решения перечисляет оба пункта и предложит установить недостающие. Если SDK и JDK установлены в нестандартные каталоги, создайте игнорируемый Git файл `AvaWeather.Android/Android.Local.props` с путями `AndroidSdkDirectory` и `JavaSdkDirectory`. Погодные ключи и файл настроек не нужны. Необязательный `appsettings.Development.json` используется только в Debug и исключён из Git. Откройте полное решение `AvaWeather.sln` в Visual Studio: фильтр `AvaWeather.DesktopWeb.slnf` исключает Android. Назначьте `AvaWeather.Android` запускаемым проектом, выберите эмулятор и нажмите F5. В Debug-сборке журнал запросов сохраняется во внутреннем каталоге приложения в `logs/api-access.log`; Release-сборка файл не создаёт. При первом запуске приложение запросит доступ к местоположению; достаточно и приблизительного доступа. Если доступ не предоставлен, погода определяется по IP. Интерфейс, локализация и погодные сервисы используются совместно с настольной и веб-версиями.

Для подписи APK в GitHub Actions уже настроены секреты `ANDROID_KEYSTORE_BASE64` и `ANDROID_KEYSTORE_PASSWORD`. Храните исходный keystore и пароль отдельно: для обновлений Android нужна та же подпись. Успешная сборка основной ветки помещает APK в GitHub Release рядом с настольными файлами. Погодные ключи в APK не встраиваются.

## Тесты

Для сборки всего решения нужны .NET 10 SDK, нагрузки `wasm-tools` и `android`, Android SDK и JDK 17. Если Android SDK пока не установлен, используйте фильтр решения для настольной и веб-версий и запускайте тестовый проект отдельно:

```sh
dotnet build AvaWeather.DesktopWeb.slnf
dotnet test Tests/Tests.csproj
```

GitHub Actions запускает тесты при каждом push и pull request в `master` или `main`, проверяет сборку Android-проекта и сохраняет TRX-отчёт. После успешных тестов основной ветки он собирает шесть автономных файлов, четыре установщика приложения и четыре пакета виджетов, создаёт GitHub Release, увеличивает версию `1.0.N` и обновляет её в README. При наличии секретов подписи к релизу добавляется Android APK. Ссылки выше всегда ведут к последнему релизу. Установщик Windows проверяется пробной установкой, сравнением исполняемого файла и удалением; пакет macOS проверяется после распаковки вместе с подписью приложения. Тесты проверяют загрузку, повтор после ошибки, защиту от параллельных запросов, фильтрацию часов по времени города, модель и содержимое виджета и отрисовку карточек в широком и узком окне. Для сохранения снимков headless-интерфейса задайте `AVAWEATHER_SCREENSHOT` и `AVAWEATHER_NARROW_SCREENSHOT` с путями к PNG.

Коммиты от `github-actions[bot]` создаёт сам GitHub Actions после выпуска релиза: он записывает новую версию в README. Пометка `[skip ci]` в сообщении такого коммита не запускает повторную сборку из-за обновления README.

Все погодные запросы настольного приложения, Android, сайта/PWA и виджетов идут через [WeatherServer](https://github.com/gagarinbefree/WeatherServer) по HTTPS. WeatherAPI, Open-Meteo и OpenWeatherMap сохраняют исходные форматы ответов; ключи используются только сервером. Сборкам AvaWeather погодные секреты не передаются.

Свежие ответы сервер кэширует на короткое время. Если погодные API недоступны, он может вернуть последний успешный ответ за последние 24 часа. Приложение сохраняет исходную дату такого ответа и показывает его как сохранённый прогноз; сначала проверяются свежие резервные источники. При отсутствии связи с сервером остаётся локальный сохранённый прогноз.

Единственная конфигурация адреса backend — `SharedAssets/Configuration/weather-server.json`. Она встраивается в .NET-библиотеку и копируется в ресурсы macOS-виджета при сборке. В репозитории AvaWeather нужны только секреты подписи Android и необязательный публичный `HAWK_DSN` для диагностики.

При переключении на OpenWeatherMap прогноз предоставляется с интервалом 3 часа, дневные показатели рассчитаны по доступным точкам прогноза, УФ и время восхода и заката следующих дней не предоставляются и обозначены прочерком. Переключение доступно при определённых координатах.

## Устройство

- `Domain` и `Application` - модель погоды, сценарий получения прогноза, интерфейсы сервисов и преобразование данных.
- `Infrastructure` - общие HTTP-клиенты WeatherAPI, Open-Meteo, OpenWeatherMap, GeoJS и геокодирования, общий HTTPS-транспорт к WeatherServer, журнал сетевых запросов в Debug и регистрация сервисов в DI. Настольные сборки включают резервное прямое подключение; браузерная сборка использует стандартный `HttpClient`.
- `AvaWeather.Browser` - статический сайт Avalonia WebAssembly для GitHub Pages.
- `AvaWeather.Android` - приложение Android с общим экраном Avalonia, MVVM-моделью и сервисами погоды.
- `AvaWeather.UI/ViewModels` - общая MVVM-модель экрана на генераторах CommunityToolkit.Mvvm.
- `AvaWeather.UI/Views` - общий интерфейс настольного приложения, Android и сайта на C# и `Avalonia.Markup.Declarative`; связи с данными созданы через `CompiledBinding`.
- `ViewLocator` - явное соответствие модели экрана и представления, создаваемого контейнером DI.

Общие SVG-иконки сайта и Avalonia-приложений лежат в `SharedAssets/Icons`. Они взяты из набора [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) под лицензией MIT. При сборке те же файлы встраиваются в `AvaWeather.UI` и используются сайтом, Android и настольным приложением без загрузки иконок из сети. Исходный рисунок и готовые форматы иконки приложения лежат в `SharedAssets/Branding` и используются настольным приложением, Android, сайтом, установщиками и виджетами.

Для собственной автономной сборки используйте `dotnet publish AvaWeather/AvaWeather.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` (или другой RID из таблицы). Погодные ключи не нужны: используется общий HTTPS-сервер.

![Русский интерфейс AvaWeather: солнечная погода, дождливые часы и разноцветный прогноз](docs/preview-weather-colors.png)
