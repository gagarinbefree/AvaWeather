using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Automation;
using AvaWeather.ViewModels;
using AvaWeather.Theming;
using Application.Theming;

namespace AvaWeather.Views;

public sealed class WeatherView : UserControl
{
    private static readonly ThemeColorService ColorService = ThemeColorService.Default;
    private static readonly IBrush Ink = ColorService.GetBrush("Ink");
    private static readonly IBrush Muted = ColorService.GetBrush("Muted");
    private static readonly IBrush Blue = ColorService.GetBrush("Blue");
    private static readonly IBrush Page = ColorService.GetBrush("Page");
    private readonly Grid _top = new() { ColumnDefinitions = new ColumnDefinitions("1*,2*"), RowDefinitions = new RowDefinitions("Auto") };
    private readonly ItemsControl _dayItems = new();
    private StackPanel _contentPanel = null!;
    private Grid _headerLayout = null!;
    private Border _headerBorder = null!;
    private StackPanel _headerActions = null!;
    private TextBlock _headerLocation = null!;
    private Grid _currentHeader = null!;
    private StackPanel _currentTemperature = null!;
    private StackPanel _hourlyTitleTexts = null!;
    private ItemsControl _hourlyItems = null!;
    private ScrollViewer _hourlyScroll = null!;
    private StackPanel _errorActions = null!;

    public WeatherView() : this(singleViewMode: false)
    {
    }

    public WeatherView(bool singleViewMode)
    {
        var current = CurrentCard();
        var hourly = HourlyCard();
        Grid.SetColumn(hourly, 1);
        _top.Children.Add(current);
        _top.Children.Add(hourly);

        var forecast = DailyCard();
        var dashboard = new DockPanel().Children(
            Header(singleViewMode),
            new ScrollViewer
            {
                Background = Page,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _contentPanel = new StackPanel
                {
                    MaxWidth = 1180,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(24, 20),
                    Spacing = 20,
                    Children =
                    {
                        LoadingPanel(),
                        ErrorPanel(),
                        _top,
                        forecast
                    }
                }
            });
        DockPanel.SetDock((Control)dashboard.Children[0], Dock.Top);
        if (singleViewMode)
        {
            Content = dashboard;
        }
        else
        {
            Content = new Grid
            {
                Children =
                {
                    dashboard,
                    ResizeGrip(WindowDecorationsElementRole.ResizeN, StandardCursorType.TopSide,
                        HorizontalAlignment.Stretch, VerticalAlignment.Top, double.NaN, 8, new Thickness(8, 0, 8, 0)),
                    ResizeGrip(WindowDecorationsElementRole.ResizeS, StandardCursorType.BottomSide,
                        HorizontalAlignment.Stretch, VerticalAlignment.Bottom, double.NaN, 8, new Thickness(8, 0, 8, 0)),
                    ResizeGrip(WindowDecorationsElementRole.ResizeW, StandardCursorType.LeftSide,
                        HorizontalAlignment.Left, VerticalAlignment.Stretch, 8, double.NaN, new Thickness(0, 8, 0, 8)),
                    ResizeGrip(WindowDecorationsElementRole.ResizeE, StandardCursorType.RightSide,
                        HorizontalAlignment.Right, VerticalAlignment.Stretch, 8, double.NaN, new Thickness(0, 8, 0, 8)),
                    ResizeGrip(WindowDecorationsElementRole.ResizeNW, StandardCursorType.TopLeftCorner,
                        HorizontalAlignment.Left, VerticalAlignment.Top, 8, 8),
                    ResizeGrip(WindowDecorationsElementRole.ResizeNE, StandardCursorType.TopRightCorner,
                        HorizontalAlignment.Right, VerticalAlignment.Top, 8, 8),
                    ResizeGrip(WindowDecorationsElementRole.ResizeSW, StandardCursorType.BottomLeftCorner,
                        HorizontalAlignment.Left, VerticalAlignment.Bottom, 8, 8),
                    ResizeGrip(WindowDecorationsElementRole.ResizeSE, StandardCursorType.BottomRightCorner,
                        HorizontalAlignment.Right, VerticalAlignment.Bottom, 8, 8)
                }
            };
        }
        SizeChanged += (_, args) => ApplyLayout(args.NewSize.Width);
        ApplyLayout(1180);
    }

    private static Border ResizeGrip(WindowDecorationsElementRole role, StandardCursorType cursor,
        HorizontalAlignment horizontal, VerticalAlignment vertical,
        double width, double height, Thickness margin = default)
    {
        var grip = new Border
        {
            Background = ColorService.GetBrush("Window.ResizeGrip"),
            Cursor = new Cursor(cursor),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Width = width,
            Height = height,
            Margin = margin
        };
        WindowDecorationProperties.SetElementRole(grip, role);
        return grip;
    }

    private Border Header(bool singleViewMode)
    {
        var location = new TextBlock
        {
            Foreground = Muted, FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
        };
        BindText<WeatherViewModel>(location, x => x.HeaderLocation);
        _headerLocation = location;
        var close = new Button
        {
            Name = "CloseWindowButton",
            Width = 32, Height = 32, Padding = new Thickness(6),
            Background = ColorService.GetBrush("Header.Close.Background"),
            BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(16),
            Content = UiIcon("dismiss_20_regular", 20, Ink)
        };
        if (!singleViewMode)
        {
            close.Bind(ToolTip.TipProperty, CompiledBinding.Create<WeatherViewModel, string>(x => x.Strings.CloseWindow));
            close.Bind(AutomationProperties.NameProperty, CompiledBinding.Create<WeatherViewModel, string>(x => x.Strings.CloseWindow));
            WindowDecorationProperties.SetElementRole(close, WindowDecorationsElementRole.User);
            close.Click += (_, _) => (TopLevel.GetTopLevel(close) as Window)?.Close();
        }

        var locationRow = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center,
            Children = { UiIcon("location_20_regular", 15, Muted), location }
        };
        var actions = _headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        actions.Children.Add(locationRow);
        if (!singleViewMode)
            actions.Children.Add(close);

        var header = _headerBorder = new Border
        {
            Name = "WeatherHeader",
            Background = WeatherCardPalette.NeutralHeader,
            Padding = new Thickness(30, 16),
            Child = _headerLayout = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children =
                {
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9, Children = { UiIcon("WeatherSunny", 22, Ink), new TextBlock { Text = "AvaWeather", Foreground = Ink, FontSize = 21, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center } } },
                    AtColumn(actions, 1)
                }
            }
        };
        header.Bind(Border.BackgroundProperty,
            CompiledBinding.Create<WeatherViewModel, IBrush>(x => x.HeaderBackground));
        if (!singleViewMode)
            WindowDecorationProperties.SetElementRole(header, WindowDecorationsElementRole.TitleBar);
        return header;
    }

    private static Control LoadingPanel()
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 14,
            Margin = new Thickness(0, 90),
            Children =
            {
                new ProgressBar { IsIndeterminate = true, Width = 180, Height = 5, Foreground = Blue },
                LocalizedText<WeatherViewModel>(x => x.Strings.LoadingWeather, 14, Muted)
            }
        };
        BindVisible<WeatherViewModel>(panel, x => x.IsLoading);
        return panel;
    }

    private static Control PlaceNameCredit()
    {
        var credit = LocalizedText<WeatherViewModel>(x => x.Strings.PlaceNameCredit, 10, Muted);
        credit.HorizontalAlignment = HorizontalAlignment.Center;
        credit.Margin = new Thickness(0, 0, 0, 12);
        BindVisible<WeatherViewModel>(credit, x => x.HasWeather);
        return credit;
    }

    private Control ErrorPanel()
    {
        var message = Label(16, Ink);
        BindText<WeatherViewModel>(message, x => x.ErrorMessage!);
        var retry = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { UiIcon("arrow_clockwise_20_regular", 15, Blue), LocalizedText<WeatherViewModel>(x => x.Strings.Retry, 14, Blue) } },
            Background = ColorService.GetBrush("White"), Foreground = Blue, Padding = new Thickness(15, 7),
            MinHeight = 44, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        retry.Bind(Button.CommandProperty, CompiledBinding.Create<WeatherViewModel, System.Windows.Input.ICommand>(x => x.LoadCommand));
        var reportLabel = LocalizedText<WeatherViewModel>(x => x.Strings.ReportIssue, 14, Blue);
        reportLabel.TextDecorations = null;
        var report = new HyperlinkButton
        {
            Name = "ReportIssueButton",
            Content = reportLabel,
            Background = ColorService.GetBrush("White"), Foreground = Blue, Padding = new Thickness(15, 7),
            MinHeight = 44, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        report.Bind(AutomationProperties.NameProperty,
            CompiledBinding.Create<WeatherViewModel, string>(x => x.Strings.ReportIssue));
        report.Bind(HyperlinkButton.NavigateUriProperty,
            CompiledBinding.Create<WeatherViewModel, Uri>(x => x.ReportIssueUri));
        var warning = UiIcon("warning_20_regular", 22, ColorService.GetBrush("Error.Icon"));
        warning.VerticalAlignment = VerticalAlignment.Top;
        var messageRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 10,
            Children = { warning, AtColumn(message, 1) }
        };
        _errorActions = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { retry, report }
        };
        var panel = new Border
        {
            Name = "WeatherErrorPanel",
            Background = ColorService.GetBrush("Error.Background"), CornerRadius = new CornerRadius(8), Padding = new Thickness(15),
            Child = new StackPanel { Spacing = 12, Children = { messageRow, _errorActions } }
        };
        BindVisible<WeatherViewModel>(panel, x => x.HasError);
        return panel;
    }

    private Control CurrentCard()
    {
        var city = Label(19, Ink);
        BindText<WeatherViewModel>(city, x => x.Display!.Location);
        var region = Label(13, Muted);
        BindText<WeatherViewModel>(region, x => x.Display!.Region);
        var updated = Label(12, Muted);
        BindText<WeatherViewModel>(updated, x => x.Display!.Updated);
        var temp = Label(34, Ink, FontWeight.Light);
        BindText<WeatherViewModel>(temp, x => x.Display!.Temperature);
        var icon = Icon<WeatherViewModel>(28, x => x.Display!.IconKind, x => x.Display!.Accent);
        var condition = Label(12, Muted);
        BindText<WeatherViewModel>(condition, x => x.Display!.Condition);

        var conditionRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 5,
            Children = { icon, AtColumn(condition, 1) }
        };
        var header = _currentHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                new StackPanel { Spacing = 3, Children = { city, region, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 8, 0, 0), Children = { UiIcon("clock_20_regular", 13, Muted), updated } } } },
                AtColumn(_currentTemperature = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Spacing = 2, Children = { temp, conditionRow } }, 1)
            }
        };

        var metrics = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = 14, ColumnSpacing = 10 };
        Metric(metrics, 0, 0, "temperature_20_regular", x => x.Strings.FeelsLike, x => x.Display!.FeelsLike);
        Metric(metrics, 1, 0, "weather_humidity_20_regular", x => x.Strings.Humidity, x => x.Display!.Humidity);
        Metric(metrics, 0, 1, "weather_squalls_20_regular", x => x.Strings.Wind, x => x.Display!.Wind);
        Metric(metrics, 1, 1, "weather_sunny_high_20_regular", x => x.Strings.UvIndex, x => x.Display!.Uv);
        Metric(metrics, 0, 2, "gauge_20_regular", x => x.Strings.Pressure, x => x.Display!.Pressure);
        Metric(metrics, 1, 2, "eye_20_regular", x => x.Strings.Visibility, x => x.Display!.Visibility);

        var card = new Border
        {
            Name = "CurrentWeatherCard",
            CornerRadius = new CornerRadius(16), Padding = new Thickness(22), Margin = new Thickness(0, 0, 10, 0), MinHeight = 295,
            Child = new StackPanel
            {
                Spacing = 17,
                Children = { header, new Border { Background = ColorService.GetBrush("Divider.Current"), Height = 1 }, metrics }
            }
        };
        card.Bind(Border.BackgroundProperty, CompiledBinding.Create<WeatherViewModel, IBrush>(x => x.Display!.CardBackground));
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static void Metric(Grid grid, int column, int row, string icon, Expression<Func<WeatherViewModel, string>> title, Expression<Func<WeatherViewModel, string>> value)
    {
        var text = Label(15, Ink);
        BindText(text, value);
        var cell = new StackPanel { Spacing = 3, Children = { IconLabel(icon, title, 13, 12, Muted), text } };
        Grid.SetColumn(cell, column);
        Grid.SetRow(cell, row);
        grid.Children.Add(cell);
    }

    private Control HourlyCard()
    {
        var items = _hourlyItems = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<HourDisplay>((_, _) => HourItem()),
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal }),
            Margin = new Thickness(0, 0, 0, 32)
        };
        items.Bind(ItemsControl.ItemsSourceProperty, CompiledBinding.Create<WeatherViewModel, IReadOnlyList<HourDisplay>>(x => x.Display!.Hourly));
        var scroll = _hourlyScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(16, 12), Content = items
        };
        var card = WhiteCard(new StackPanel
        {
            Children =
            {
                SectionTitle("clock_20_regular", x => x.Strings.HourlyForecast, x => x.Strings.TodayTomorrow),
                scroll
            }
        });
        card.Name = "HourlyForecastCard";
        card.MinHeight = 295;
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static Control HourItem()
    {
        var time = Label(14, Ink); BindText<HourDisplay>(time, x => x.Time);
        var icon = Icon<HourDisplay>(40, x => x.IconKind, x => x.Accent);
        var temp = Label(16, Ink, FontWeight.Bold); BindText<HourDisplay>(temp, x => x.Temperature);
        var condition = Label(11, Muted); BindText<HourDisplay>(condition, x => x.Condition);
        var rain = Label(11, Muted); BindText<HourDisplay>(rain, x => x.Rain);
        var rainRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center, Children = { UiIcon("drop_20_regular", 12, Muted), rain } };
        BindVisible<HourDisplay>(rainRow, x => x.HasRain);
        var now = LocalizedText<HourDisplay>(x => x.Strings.Now, 11, Blue);
        now.HorizontalAlignment = HorizontalAlignment.Center;
        BindVisible<HourDisplay>(now, x => x.IsNow);
        var card = new Border
        {
            MinWidth = 88, MaxWidth = 105, CornerRadius = new CornerRadius(10), Padding = new Thickness(8), Margin = new Thickness(3, 0, 8, 0),
            Child = new StackPanel { Spacing = 7, HorizontalAlignment = HorizontalAlignment.Center, Children = { time, icon, temp, condition, rainRow, now } }
        };
        card.Bind(Border.BackgroundProperty, CompiledBinding.Create<HourDisplay, IBrush>(x => x.CardBackground));
        return card;
    }

    private Control DailyCard()
    {
        var items = _dayItems;
        items.ItemTemplate = new FuncDataTemplate<DayDisplay>((_, _) => DayItem());
        items.Bind(ItemsControl.ItemsSourceProperty, CompiledBinding.Create<WeatherViewModel, IReadOnlyList<DayDisplay>>(x => x.Display!.Daily));
        items.ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Columns = 3 });
        var card = WhiteCard(new StackPanel { Children = { SectionTitle("calendar_20_regular", x => x.Strings.ThreeDayForecast), new Border { Padding = new Thickness(12), Child = items } } });
        card.Name = "DailyForecastCard";
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static Control DayItem()
    {
        var name = Label(15, Blue, FontWeight.SemiBold); BindText<DayDisplay>(name, x => x.Name);
        var date = Label(12, Muted); BindText<DayDisplay>(date, x => x.Date);
        var icon = Icon<DayDisplay>(50, x => x.IconKind, x => x.Accent);
        var max = Label(19, Ink, FontWeight.Bold); BindText<DayDisplay>(max, x => x.Maximum);
        var min = Label(12, Muted); BindText<DayDisplay>(min, x => x.Minimum);
        var condition = Label(14, Ink); BindText<DayDisplay>(condition, x => x.Condition);
        var stats = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = 9, ColumnSpacing = 8 };
        DayMetric(stats, 0, 0, "weather_humidity_20_regular", x => x.Strings.Humidity, x => x.Humidity);
        DayMetric(stats, 1, 0, "weather_squalls_20_regular", x => x.Strings.Wind, x => x.Wind);
        DayMetric(stats, 0, 1, "WeatherRain", x => x.Strings.Rain, x => x.Rain);
        DayMetric(stats, 1, 1, "weather_sunny_high_20_regular", x => x.Strings.Uv, x => x.Uv);
        DayMetric(stats, 0, 2, "weather_sunny_low_20_regular", x => x.Strings.SunriseSunset, x => x.SunriseSunset);
        var card = new Border
        {
            Name = "DayForecastItem",
            MaxWidth = 310, MinHeight = 255, CornerRadius = new CornerRadius(10), Padding = new Thickness(17), Margin = new Thickness(5, 0, 12, 12),
            Child = new StackPanel
            {
                Spacing = 9,
                Children =
                {
                    new StackPanel { Spacing = 2, Children = { name, date } },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { icon, new StackPanel { Children = { max, min } } } },
                    condition, stats
                }
            }
        };
        card.Bind(Border.BackgroundProperty, CompiledBinding.Create<DayDisplay, IBrush>(x => x.CardBackground));
        return card;
    }

    private static void DayMetric(Grid grid, int column, int row, string icon, Expression<Func<DayDisplay, string>> title, Expression<Func<DayDisplay, string>> value)
    {
        var text = Label(12, Ink); BindText(text, value);
        var cell = new StackPanel { Children = { IconLabel(icon, title, 11, 11, Muted), text } };
        Grid.SetColumn(cell, column); Grid.SetRow(cell, row); grid.Children.Add(cell);
    }

    private static Border WhiteCard(Control content) => new()
    {
        Background = ColorService.GetBrush("White"), CornerRadius = new CornerRadius(16), Child = content
    };

    private Control SectionTitle(string icon, Expression<Func<WeatherViewModel, string>> title, Expression<Func<WeatherViewModel, string>>? subtitle = null)
    {
        var titleText = LocalizedText(title, 18, Ink, FontWeight.SemiBold);
        var subtitleText = new TextBlock { FontSize = 12, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        if (subtitle is not null) BindText(subtitleText, subtitle);
        var texts = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Children = { titleText, subtitleText }
        };
        if (subtitle is not null)
            _hourlyTitleTexts = texts;
        return new Border
        {
            BorderBrush = ColorService.GetBrush("Divider.Section"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(18, 16),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 8,
                Children = { UiIcon(icon, 18, Blue), texts }
            }
        };
    }

    private static StackPanel IconLabel<T>(string icon, Expression<Func<T, string>> title, double iconSize, double fontSize, IBrush color) => new()
    {
        Orientation = Orientation.Horizontal, Spacing = 4,
        Children = { UiIcon(icon, iconSize, color), LocalizedText(title, fontSize, color) }
    };

    private static TextBlock LocalizedText<T>(Expression<Func<T, string>> value, double size, IBrush color, FontWeight weight = FontWeight.Normal)
    {
        var text = new TextBlock { FontSize = size, Foreground = color, FontWeight = weight, VerticalAlignment = VerticalAlignment.Center };
        BindText(text, value);
        return text;
    }

    private static SvgIcon UiIcon(string kind, double size, IBrush color) => new(kind, size, color);

    private static TextBlock Label(double size, IBrush color, FontWeight weight = FontWeight.Normal) => new()
    {
        FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap
    };

    private static WeatherIcon Icon<T>(double size, Expression<Func<T, WeatherConditionKind>> kind, Expression<Func<T, IBrush>> accent)
    {
        var icon = new WeatherIcon(size);
        icon.Bind(WeatherIcon.KindProperty, CompiledBinding.Create(kind));
        icon.Bind(WeatherIcon.AccentBrushProperty, CompiledBinding.Create(accent));
        return icon;
    }

    private static T AtColumn<T>(T control, int column) where T : Control { Grid.SetColumn(control, column); return control; }
    private static T AtDock<T>(T control, Dock dock) where T : Control { DockPanel.SetDock(control, dock); return control; }

    private static void BindText<T>(TextBlock control, Expression<Func<T, string>> property) =>
        control.Text(CompiledBinding.Create(property));

    private static void BindVisible<T>(Control control, Expression<Func<T, bool>> property) =>
        control.IsVisible(CompiledBinding.Create(property));

    private void ApplyLayout(double width)
    {
        var dayColumns = width >= 1050 ? 3 : width >= 740 ? 2 : 1;
        _dayItems.ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Columns = dayColumns });
        if (width < 480)
        {
            _errorActions.Orientation = width < 380 ? Orientation.Vertical : Orientation.Horizontal;
            _errorActions.HorizontalAlignment = width < 380
                ? HorizontalAlignment.Stretch : HorizontalAlignment.Right;
            _contentPanel.Margin = new Thickness(12, 12);
            _contentPanel.Spacing = 12;
            _headerBorder.Padding = new Thickness(16, 12);
            _headerLayout.ColumnDefinitions = new ColumnDefinitions("*");
            _headerLayout.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(_headerActions, 0);
            Grid.SetRow(_headerActions, 1);
            _headerActions.Margin = new Thickness(0, 8, 0, 0);
            _headerLocation.MaxWidth = Math.Max(120, width - 52);
            _currentHeader.ColumnDefinitions = new ColumnDefinitions("*");
            _currentHeader.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(_currentTemperature, 0);
            Grid.SetRow(_currentTemperature, 1);
            _currentTemperature.HorizontalAlignment = HorizontalAlignment.Stretch;
            _currentTemperature.Margin = new Thickness(0, 10, 0, 0);
            ((Border)_top.Children[0]).Padding = new Thickness(16);
            ((Border)_top.Children[0]).MinHeight = 0;
            ((Border)_top.Children[1]).MinHeight = 0;
            _hourlyTitleTexts.Orientation = Orientation.Vertical;
            _hourlyTitleTexts.Spacing = 2;
            _hourlyTitleTexts.MaxWidth = Math.Max(100, width - 86);
            _hourlyItems.Margin = new Thickness(0, 0, 0, 12);
            _hourlyScroll.Margin = new Thickness(12, 10);
        }
        else
        {
            _errorActions.Orientation = Orientation.Horizontal;
            _errorActions.HorizontalAlignment = HorizontalAlignment.Right;
            _contentPanel.Margin = new Thickness(24, 20);
            _contentPanel.Spacing = 20;
            _headerBorder.Padding = new Thickness(30, 16);
            _headerLayout.ColumnDefinitions = new ColumnDefinitions("*,Auto");
            _headerLayout.RowDefinitions = new RowDefinitions("Auto");
            Grid.SetColumn(_headerActions, 1);
            Grid.SetRow(_headerActions, 0);
            _headerActions.Margin = new Thickness(0);
            _headerLocation.MaxWidth = double.PositiveInfinity;
            _currentHeader.ColumnDefinitions = new ColumnDefinitions("*,Auto");
            _currentHeader.RowDefinitions = new RowDefinitions("Auto");
            Grid.SetColumn(_currentTemperature, 1);
            Grid.SetRow(_currentTemperature, 0);
            _currentTemperature.HorizontalAlignment = HorizontalAlignment.Right;
            _currentTemperature.Margin = new Thickness(0);
            ((Border)_top.Children[0]).Padding = new Thickness(22);
            ((Border)_top.Children[0]).MinHeight = 295;
            ((Border)_top.Children[1]).MinHeight = 295;
            _hourlyTitleTexts.Orientation = Orientation.Horizontal;
            _hourlyTitleTexts.Spacing = 8;
            _hourlyTitleTexts.MaxWidth = double.PositiveInfinity;
            _hourlyItems.Margin = new Thickness(0, 0, 0, 32);
            _hourlyScroll.Margin = new Thickness(16, 12);
        }
        if (width < 800)
        {
            _top.ColumnDefinitions = new ColumnDefinitions("*");
            _top.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(_top.Children[1], 0);
            Grid.SetRow(_top.Children[1], 1);
            ((Border)_top.Children[0]).Margin = new Thickness(0, 0, 0, width < 480 ? 12 : 18);
        }
        else
        {
            _top.ColumnDefinitions = new ColumnDefinitions("1*,2*");
            _top.RowDefinitions = new RowDefinitions("Auto");
            Grid.SetColumn(_top.Children[1], 1);
            Grid.SetRow(_top.Children[1], 0);
            ((Border)_top.Children[0]).Margin = new Thickness(0, 0, 10, 0);
        }
    }
}
