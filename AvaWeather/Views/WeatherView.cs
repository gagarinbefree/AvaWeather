using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using AvaWeather.ViewModels;

namespace AvaWeather.Views;

public sealed class WeatherView : UserControl
{
    private static readonly IBrush Ink = Brush.Parse("#26334A");
    private static readonly IBrush Muted = Brush.Parse("#718096");
    private static readonly IBrush Blue = Brush.Parse("#1A237E");
    private static readonly IBrush PaleBlue = Brush.Parse("#E8ECFA");
    private static readonly IBrush Page = Brush.Parse("#F5F7FA");
    private readonly Grid _top = new() { ColumnDefinitions = new ColumnDefinitions("1*,2*"), RowDefinitions = new RowDefinitions("Auto") };
    private readonly ItemsControl _dayItems = new();

    public WeatherView()
    {
        var current = CurrentCard();
        var hourly = HourlyCard();
        Grid.SetColumn(hourly, 1);
        _top.Children.Add(current);
        _top.Children.Add(hourly);

        var forecast = DailyCard();
        Content = new DockPanel().Children(
            Header(),
            new ScrollViewer
            {
                Background = Page,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new StackPanel
                {
                    MaxWidth = 1180,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(24, 28),
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
        DockPanel.SetDock((Control)((DockPanel)Content).Children[0], Dock.Top);
        SizeChanged += (_, args) => ApplyLayout(args.NewSize.Width);
        ApplyLayout(1180);
    }

    private static Border Header() => new()
    {
        Background = HeaderGradient(),
        Padding = new Thickness(30, 16),
        Child = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                new TextBlock { Text = "☀  PwrsWeather", Foreground = Brushes.White, FontSize = 21, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
                AtColumn(new TextBlock { Text = "⌖  Moscow", Foreground = Brush.Parse("#BEC8E4"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, 1)
            }
        }
    };

    private static LinearGradientBrush HeaderGradient() => new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = { new GradientStop(Color.Parse("#1A237E"), 0), new GradientStop(Color.Parse("#0D47A1"), 1) }
    };

    private static LinearGradientBrush CardGradient() => new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops = { new GradientStop(Color.Parse("#1A237E"), 0), new GradientStop(Color.Parse("#283593"), 1) }
    };

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
                new TextBlock { Text = "Loading weather...", Foreground = Muted, HorizontalAlignment = HorizontalAlignment.Center }
            }
        };
        BindVisible<WeatherViewModel>(panel, x => x.IsLoading);
        return panel;
    }

    private static Control ErrorPanel()
    {
        var message = Label(16, Ink);
        BindText<WeatherViewModel>(message, x => x.ErrorMessage!);
        var button = new Button { Content = "↻  Retry", Background = Brushes.White, Foreground = Blue, Padding = new Thickness(15, 7) };
        button.Bind(Button.CommandProperty, CompiledBinding.Create<WeatherViewModel, System.Windows.Input.ICommand>(x => x.LoadCommand));
        var panel = new Border
        {
            Background = Brush.Parse("#F8D7DA"), CornerRadius = new CornerRadius(8), Padding = new Thickness(15),
            Child = new DockPanel { LastChildFill = true, Children = { AtDock(button, Dock.Right), new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { new TextBlock { Text = "⚠", FontSize = 23, Foreground = Brush.Parse("#A61B29") }, message } } } }
        };
        BindVisible<WeatherViewModel>(panel, x => x.HasError);
        return panel;
    }

    private static Control CurrentCard()
    {
        var city = Label(19, Brush.Parse("#CDD4EF"));
        BindText<WeatherViewModel>(city, x => x.Display!.Location);
        var region = Label(13, Brush.Parse("#CDD4EF"));
        BindText<WeatherViewModel>(region, x => x.Display!.Region);
        var updated = Label(12, Brush.Parse("#CDD4EF"));
        BindText<WeatherViewModel>(updated, x => x.Display!.Updated);
        var temp = Label(34, Brushes.White, FontWeight.Light);
        BindText<WeatherViewModel>(temp, x => x.Display!.Temperature);
        var icon = Icon<WeatherViewModel>(28, x => x.Display!.Icon, x => x.Display!.IconUrl);
        var condition = Label(12, Brush.Parse("#CDD4EF"));
        BindText<WeatherViewModel>(condition, x => x.Display!.Condition);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                new StackPanel { Spacing = 3, Children = { city, region, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 8, 0, 0), Children = { new TextBlock { Text = "◷", Foreground = Brush.Parse("#CDD4EF") }, updated } } } },
                AtColumn(new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Spacing = 2, Children = { temp, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Children = { icon, condition } } } }, 1)
            }
        };

        var metrics = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = 14, ColumnSpacing = 10 };
        Metric(metrics, 0, 0, "♨  Feels Like", x => x.Display!.FeelsLike);
        Metric(metrics, 1, 0, "💧  Humidity", x => x.Display!.Humidity);
        Metric(metrics, 0, 1, "≋  Wind", x => x.Display!.Wind);
        Metric(metrics, 1, 1, "☀  UV Index", x => x.Display!.Uv);
        Metric(metrics, 0, 2, "◉  Pressure", x => x.Display!.Pressure);
        Metric(metrics, 1, 2, "◉  Visibility", x => x.Display!.Visibility);

        var card = new Border
        {
            Background = CardGradient(), CornerRadius = new CornerRadius(16), Padding = new Thickness(22), Margin = new Thickness(0, 0, 10, 0), MinHeight = 295,
            Child = new StackPanel
            {
                Spacing = 17,
                Children = { header, new Border { Background = Brush.Parse("#4A5A9D"), Height = 1 }, metrics }
            }
        };
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static void Metric(Grid grid, int column, int row, string title, Expression<Func<WeatherViewModel, string>> value)
    {
        var text = Label(15, Brushes.White);
        BindText(text, value);
        var cell = new StackPanel { Spacing = 3, Children = { new TextBlock { Text = title, FontSize = 12, Foreground = Brush.Parse("#CDD4EF") }, text } };
        Grid.SetColumn(cell, column);
        Grid.SetRow(cell, row);
        grid.Children.Add(cell);
    }

    private static Control HourlyCard()
    {
        var items = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<HourDisplay>((_, _) => HourItem()),
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal })
        };
        items.Bind(ItemsControl.ItemsSourceProperty, CompiledBinding.Create<WeatherViewModel, IReadOnlyList<HourDisplay>>(x => x.Display!.Hourly));
        var card = WhiteCard(new StackPanel
        {
            Children =
            {
                SectionTitle("◷  Hourly Forecast", "Today & Tomorrow"),
                new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(16, 12), Content = items }
            }
        });
        card.MinHeight = 295;
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static Control HourItem()
    {
        var time = Label(14, Ink); BindText<HourDisplay>(time, x => x.Time);
        var icon = Icon<HourDisplay>(40, x => x.Icon, x => x.IconUrl);
        var temp = Label(16, Ink, FontWeight.Bold); BindText<HourDisplay>(temp, x => x.Temperature);
        var condition = Label(11, Muted); BindText<HourDisplay>(condition, x => x.Condition);
        var rain = Label(11, Muted); BindText<HourDisplay>(rain, x => x.Rain);
        var now = new TextBlock { Text = "Now", FontSize = 11, Foreground = Blue, HorizontalAlignment = HorizontalAlignment.Center };
        BindVisible<HourDisplay>(now, x => x.IsNow);
        return new Border
        {
            MinWidth = 88, MaxWidth = 105, CornerRadius = new CornerRadius(10), Padding = new Thickness(8), Margin = new Thickness(3, 0, 8, 0), Background = PaleBlue,
            Child = new StackPanel { Spacing = 7, HorizontalAlignment = HorizontalAlignment.Center, Children = { time, icon, temp, condition, rain, now } }
        };
    }

    private Control DailyCard()
    {
        var items = _dayItems;
        items.ItemTemplate = new FuncDataTemplate<DayDisplay>((_, _) => DayItem());
        items.Bind(ItemsControl.ItemsSourceProperty, CompiledBinding.Create<WeatherViewModel, IReadOnlyList<DayDisplay>>(x => x.Display!.Daily));
        items.ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid { Columns = 3 });
        var card = WhiteCard(new StackPanel { Children = { SectionTitle("▦  3-Day Forecast"), new Border { Padding = new Thickness(12), Child = items } } });
        BindVisible<WeatherViewModel>(card, x => x.HasWeather);
        return card;
    }

    private static Control DayItem()
    {
        var name = Label(15, Blue, FontWeight.SemiBold); BindText<DayDisplay>(name, x => x.Name);
        var date = Label(12, Muted); BindText<DayDisplay>(date, x => x.Date);
        var icon = Icon<DayDisplay>(50, x => x.Icon, x => x.IconUrl);
        var max = Label(19, Ink, FontWeight.Bold); BindText<DayDisplay>(max, x => x.Maximum);
        var min = Label(12, Muted); BindText<DayDisplay>(min, x => x.Minimum);
        var condition = Label(14, Ink); BindText<DayDisplay>(condition, x => x.Condition);
        var stats = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = 9, ColumnSpacing = 8 };
        DayMetric(stats, 0, 0, "💧 Humidity", x => x.Humidity);
        DayMetric(stats, 1, 0, "≋ Wind", x => x.Wind);
        DayMetric(stats, 0, 1, "☂ Rain", x => x.Rain);
        DayMetric(stats, 1, 1, "☀ UV", x => x.Uv);
        DayMetric(stats, 0, 2, "◑ Sunrise / Sunset", x => x.SunriseSunset);
        return new Border
        {
            Width = 310, MinHeight = 255, Background = Page, CornerRadius = new CornerRadius(10), Padding = new Thickness(17), Margin = new Thickness(5, 0, 12, 12),
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
    }

    private static void DayMetric(Grid grid, int column, int row, string title, Expression<Func<DayDisplay, string>> value)
    {
        var text = Label(12, Ink); BindText(text, value);
        var cell = new StackPanel { Children = { new TextBlock { Text = title, FontSize = 11, Foreground = Muted }, text } };
        Grid.SetColumn(cell, column); Grid.SetRow(cell, row); grid.Children.Add(cell);
    }

    private static Border WhiteCard(Control content) => new()
    {
        Background = Brushes.White, CornerRadius = new CornerRadius(16), Child = content
    };

    private static Control SectionTitle(string title, string? subtitle = null) => new Border
    {
        BorderBrush = Brush.Parse("#EEF0F5"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(18, 16),
        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            Children = { new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Ink }, new TextBlock { Text = subtitle ?? "", FontSize = 12, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center } }
        }
    };

    private static TextBlock Label(double size, IBrush color, FontWeight weight = FontWeight.Normal) => new()
    {
        FontSize = size, Foreground = color, FontWeight = weight, TextWrapping = TextWrapping.Wrap
    };

    private static WeatherIcon Icon<T>(double size, Expression<Func<T, string>> symbol, Expression<Func<T, string>> url)
    {
        var icon = new WeatherIcon(size);
        icon.Bind(WeatherIcon.SymbolProperty, CompiledBinding.Create(symbol));
        icon.Bind(WeatherIcon.UrlProperty, CompiledBinding.Create(url));
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
        if (width < 800)
        {
            _top.ColumnDefinitions = new ColumnDefinitions("*");
            _top.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetColumn(_top.Children[1], 0);
            Grid.SetRow(_top.Children[1], 1);
            ((Border)_top.Children[0]).Margin = new Thickness(0, 0, 0, 18);
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
