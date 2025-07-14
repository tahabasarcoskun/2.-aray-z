using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.IO.Ports;
using System;
using copilot_deneme.ViewModels;
using System.Globalization;
using System.Text;

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

            // Serial data received event'i için delegate ekle
            SerialPortService.OnDataReceived += OnSerialDataReceived;
            
            System.Diagnostics.Debug.WriteLine("HomePage baþlatýldý - Telemetri iþleme sutPage'de gerçekleþtirilecek");
        }

        private void OnSerialDataReceived(string data)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Gelen veriyi TextBox'a ekle - sadece ham veri gösterimi
                SerialDataTextBox.Text += $"{DateTime.Now:HH:mm:ss}: {data}\n";

                // TextBox'ý en alta kaydýr
                SerialDataScrollViewer.ChangeView(null, SerialDataScrollViewer.ExtentHeight, null);
                
                // Status güncelle
                StatusTextBlock.Text = $"Ham veri alýndý: {DateTime.Now:HH:mm:ss}";
                
                System.Diagnostics.Debug.WriteLine($"HomePage - Ham veri görüntülendi: {data}");
            });
        }

        // Temizle butonu click handler'ý
        private void ClearData_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SerialDataTextBox.Text = "";
            StatusTextBlock.Text = "Veriler temizlendi";
            System.Diagnostics.Debug.WriteLine("HomePage verileri temizlendi");
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SerialPortService.OnDataReceived -= OnSerialDataReceived;
            System.Diagnostics.Debug.WriteLine("HomePage'den ayrýldý - Event handler'lar temizlendi");
        }
    }
}