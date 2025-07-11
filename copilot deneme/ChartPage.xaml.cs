using copilot_deneme.ViewModels;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace copilot_deneme
{
    public sealed partial class ChartPage : Page
    {
        // ViewModel'i doðrudan servisten alýyoruz
        public ChartViewModel ViewModel => SerialPortService.ViewModel;

        public ChartPage()
        {
            this.InitializeComponent();
            // DataContext'i ayarlamaya gerek yok, XAML tarafýnda {x:Bind} ile doðrudan ViewModel'e baðlanacaðýz.
            AltitudeSeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
            SpeedSeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
            AccelSeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
            TempSeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
            PressureySeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
            HumiditySeries.LegendTextPaint = new SolidColorPaint(SKColors.White, 10);
        }
    }
}