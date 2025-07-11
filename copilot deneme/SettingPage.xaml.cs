using System.IO.Ports;
using copilot_deneme.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace copilot_deneme
{
    public sealed partial class SettingPage : Page
    {
        private ChartViewModel _viewModel;
        
        public SettingPage()
        {
            this.InitializeComponent();
            var viewModel = new ChartViewModel();
            this.DataContext = viewModel;
            _viewModel = viewModel; // Ayný instance'ý kullan
            
            SerialPortService.ViewModel = _viewModel;
            SerialPortService.Dispatcher = DispatcherQueue.GetForCurrentThread();
            
            PortComboBox.ItemsSource = SerialPort.GetPortNames();
            BaudRateComboBox.SelectedIndex = 0;
        }

        private void OpenPort_Click(object sender, RoutedEventArgs e)
        {
            var portName = PortComboBox.SelectedItem as string;
            var baudRate = int.Parse((BaudRateComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "9600");

            if (string.IsNullOrEmpty(portName))
                return;

            try
            {
                // Önce mevcut baðlantýyý kapat
                if (SerialPortService.SerialPort != null && SerialPortService.SerialPort.IsOpen)
                {
                    SerialPortService.StopReading();
                }

                // Yeni port oluþtur ve aç
                SerialPortService.SerialPort = new SerialPort(portName, baudRate);
                SerialPortService.SerialPort.Open();
                
                // ÖNEMLÝ: Veri okumayý baþlat
                SerialPortService.StartReading();

                DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    StatusText.Text = $"Port açýldý: {portName} - {baudRate}";
                });
                
                System.Diagnostics.Debug.WriteLine($"Port opened and reading started: {portName}");
            }
            catch (System.Exception ex)
            {
                DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    StatusText.Text = $"Hata: {ex.Message}";
                });
                System.Diagnostics.Debug.WriteLine($"Error opening port: {ex.Message}");
            }
        }
        
        private void ClosePort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortService.StopReading();
                DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    StatusText.Text = "Port kapatýldý";
                });
            }
            catch (System.Exception ex)
            {
                DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    StatusText.Text = $"Hata: {ex.Message}";
                });
            }
        }
    }
}