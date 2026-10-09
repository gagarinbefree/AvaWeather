using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWeather.Views;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "AvaWeather";
        Width = 1180;
        Height = 810;
        MinWidth = 490;
        MinHeight = 520;
        Background = new SolidColorBrush(Color.Parse("#F5F7FA"));
    }
}
