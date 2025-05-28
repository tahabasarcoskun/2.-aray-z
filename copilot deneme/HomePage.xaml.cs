using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.IO.Ports;
using System;
using copilot_deneme.ViewModels;

namespace copilot_deneme
{
    public sealed partial class HomePage : Page
    {
        private readonly ChartViewModel _viewModel;
        private readonly DispatcherQueue _dispatcherQueue;

        public HomePage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _viewModel = new ChartViewModel();
            this.DataContext = _viewModel;

            // SerialPortService'i yapýlandýr
            SerialPortService.ViewModel = _viewModel;
            SerialPortService.Dispatcher = _dispatcherQueue;

            // Event handler'ý SerialPortService'e býrak, burada duplicate etme
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Sayfa yüklendiðinde SerialPort durumunu kontrol et ve okumaya baþla
            if (SerialPortService.IsPortOpen())
            {
                System.Diagnostics.Debug.WriteLine("HomePage: SerialPort is open, starting reading...");
                SerialPortService.StartReading();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HomePage: SerialPort is not open");
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Sayfa deðiþtiðinde okumayý durdurmak istemiyorsan bu satýrý yorum satýrý yap
            // SerialPortService.StopReading();

            System.Diagnostics.Debug.WriteLine("HomePage: Navigated away from page");
        }

        // Debug için manuel test butonu (opsiyonel - XAML'de buton varsa kullan)
        private void TestButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Manual test - adding sample data");
            _viewModel.AddLdrValue(500);
            _viewModel.AddDistanceValue(25.5);
        }

        // SerialPort durumunu kontrol etmek için yardýmcý metod
        private void CheckSerialPortStatus()
        {
            var portInfo = SerialPortService.GetPortInfo();
            System.Diagnostics.Debug.WriteLine($"HomePage - Serial Port Info: {portInfo}");
        }
    }
}