using copilot_deneme.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO.Ports;
using System.Linq;
using Windows.UI; // This is already present but Colors class needs to be accessed differently

namespace copilot_deneme
{
    public sealed partial class SettingPage : Page
    {
        private ChartViewModel _viewModel;
        private DispatcherQueue _dispatcherQueue;
        public SettingPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            
            var viewModel = new ChartViewModel();
            this.DataContext = viewModel;
            _viewModel = viewModel; // Aynı instance'ı kullan
            
            SerialPortService.ViewModel = _viewModel;
            SerialPortService.Dispatcher = _dispatcherQueue;


            SerialPortService.OnHYIPacketReceived += OnHYIPacketReceived;
            // İlk yüklemede portları doldur
            RefreshAvailablePorts();
            BaudRateComboBox_Input.SelectedIndex = 0;
            BaudRateComboBox_Output.SelectedIndex = 0;
            
  
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshAvailablePorts();
           
            System.Diagnostics.Debug.WriteLine("Ports refreshed");
        }
        private void OnHYIPacketReceived(SerialPortService.HYITelemetryData data)
        {
            // HYI verisi alındığında log
            _dispatcherQueue.TryEnqueue(() =>
            {
                System.Diagnostics.Debug.WriteLine($"SettingPage: HYI Data - Team: {data.TeamId}, Counter: {data.PacketCounter}");
            });
        }

        private void RefreshAvailablePorts()
        {
            string[] availablePorts = SerialPort.GetPortNames();
            var inputSelection = PortComboBox_Input.SelectedItem as string;
            var outputSelection = PortComboBox_Output.SelectedItem as string;

            PortComboBox_Input.ItemsSource = availablePorts;
            PortComboBox_Output.ItemsSource = availablePorts;

            if (availablePorts.Contains(inputSelection)) PortComboBox_Input.SelectedItem = inputSelection;
            else if (availablePorts.Length > 0) PortComboBox_Input.SelectedIndex = 0;

            if (availablePorts.Contains(outputSelection)) PortComboBox_Output.SelectedItem = outputSelection;
            else if (availablePorts.Length > 1) PortComboBox_Output.SelectedIndex = 1;
            else if (availablePorts.Length > 0) PortComboBox_Output.SelectedIndex = 0;

            StatusText_Input.Text = $"{availablePorts.Length} port bulundu.";
            StatusText_Output.Text = $"{availablePorts.Length} port bulundu.";
        }

        private void ConnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Input.SelectedItem as string;
                if (string.IsNullOrEmpty(portName)) throw new InvalidOperationException("Giriş için bir port seçin.");
                var baudRate = int.Parse((BaudRateComboBox_Input.SelectedItem as ComboBoxItem).Content.ToString());

                // SerialPortService'i kullan
                SerialPortService.Initialize(portName, baudRate);
                SerialPortService.StartReading();

                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Input.Text = $"Bağlandı: {portName}";
            }
            catch (Exception ex)
            {
                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Input.Text = $"Hata: {ex.Message}";
            }
        }

        private void DisconnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            SerialPortService.StopReading();
            StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
            StatusText_Input.Text = "Giriş Portu Kapalı";
        }

        private void ConnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Output.SelectedItem as string;
                if (string.IsNullOrEmpty(portName))
                    throw new InvalidOperationException("Çıkış için bir port seçin.");

                var baudRate = int.Parse((BaudRateComboBox_Output.SelectedItem as ComboBoxItem).Content.ToString());

                SerialPortService.InitializeOutputPort(portName, baudRate);

                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Output.Text = $"HYI Output Aktif: {portName} ({baudRate})";

                System.Diagnostics.Debug.WriteLine($"Output port connected: {portName}");
            }
            catch (Exception ex)
            {
                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Output.Text = $"Output Hatası: {ex.Message}";
            }
        }

        private void DisconnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortService.CloseOutputPort();
                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Output.Text = "Çıkış Portu Kapalı";
            }
            catch (Exception ex)
            {
                StatusText_Output.Text = $"Kapatma Hatası: {ex.Message}";
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // Cleanup if needed
            SerialPortService.OnHYIPacketReceived -= OnHYIPacketReceived;
        }
    }
}