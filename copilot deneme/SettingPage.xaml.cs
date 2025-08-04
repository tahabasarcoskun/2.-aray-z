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
            string[] availablePorts = SerialPortService.GetAvailablePorts();
            string[] availableOutputPorts = SerialPortService.GetAvailableOutputPorts();

            var inputSelection = PortComboBox_Input.SelectedItem as string;
            var outputSelection = PortComboBox_Output.SelectedItem as string;

            PortComboBox_Input.ItemsSource = availablePorts;
            PortComboBox_Output.ItemsSource = availableOutputPorts;

            // Input port selection logic
            if (availablePorts.Contains(inputSelection))
            {
                PortComboBox_Input.SelectedItem = inputSelection;
            }
            else if (availablePorts.Length > 0)
            {
                PortComboBox_Input.SelectedIndex = 0;
            }
            else
            {
                PortComboBox_Input.SelectedIndex = -1;
            }

            // Output port selection logic - ensure it's different from input port if possible
            if (availableOutputPorts.Contains(outputSelection))
            {
                PortComboBox_Output.SelectedItem = outputSelection;
            }
            else if (availableOutputPorts.Length > 0)
            {
                PortComboBox_Output.SelectedIndex = 0;
            }
            else
            {
                // No output ports available (either no ports or all are used by input)
                PortComboBox_Output.SelectedIndex = -1;
            }

            // Update status messages
            StatusText_Input.Text = availablePorts.Length > 0
                ? $"{availablePorts.Length} port bulundu."
                : "Hiç port bulunamadı.";

            StatusText_Output.Text = availableOutputPorts.Length > 0
                ? $"{availableOutputPorts.Length} çıkış portu mevcut."
                : (availablePorts.Length > 0
                    ? "Çıkış için ek port gerekli (giriş portu kullanımda)."
                    : "Hiç port bulunamadı.");

            // Debug log available ports
            System.Diagnostics.Debug.WriteLine($"Available input ports: {string.Join(", ", availablePorts)}");
            System.Diagnostics.Debug.WriteLine($"Available output ports: {string.Join(", ", availableOutputPorts)}");
        }

        private void ConnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Input.SelectedItem as string;
                if (string.IsNullOrEmpty(portName))
                {
                    throw new InvalidOperationException("Giriş için bir port seçin.");
                }

                // Verify port exists
                if (!SerialPortService.GetAvailablePorts().Contains(portName))
                {
                    throw new InvalidOperationException($"Port {portName} mevcut değil. Portları yenileyin.");
                }

                var baudRate = int.Parse((BaudRateComboBox_Input.SelectedItem as ComboBoxItem).Content.ToString());

                // SerialPortService'i kullan
                SerialPortService.Initialize(portName, baudRate);
                SerialPortService.StartReading();

                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Input.Text = $"Bağlandı: {portName}";

                // Refresh output ports to exclude the newly connected input port
                RefreshAvailablePorts();

                System.Diagnostics.Debug.WriteLine($"Input port connected successfully: {portName}");
            }
            catch (Exception ex)
            {
                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Input.Text = $"Hata: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Input port connection failed: {ex.Message}");
            }
        }

        private void DisconnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortService.StopReading();
                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Input.Text = "Giriş Portu Kapalı";

                // Refresh ports to make the disconnected port available for output
                RefreshAvailablePorts();

                System.Diagnostics.Debug.WriteLine("Input port disconnected");
            }
            catch (Exception ex)
            {
                StatusText_Input.Text = $"Kapatma Hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Input port disconnect failed: {ex.Message}");
            }
        }

        private void ConnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Output.SelectedItem as string;
                if (string.IsNullOrEmpty(portName))
                {
                    throw new InvalidOperationException("Çıkış için bir port seçin.");
                }

                // Verify port exists in available output ports
                if (!SerialPortService.GetAvailableOutputPorts().Contains(portName))
                {
                    throw new InvalidOperationException($"Port {portName} çıkış için mevcut değil. Portları yenileyin.");
                }

                var baudRate = int.Parse((BaudRateComboBox_Output.SelectedItem as ComboBoxItem).Content.ToString());

                SerialPortService.InitializeOutputPort(portName, baudRate);

                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Output.Text = $"HYI Output Aktif: {portName} ({baudRate})";

                System.Diagnostics.Debug.WriteLine($"Output port connected successfully: {portName}");
            }
            catch (Exception ex)
            {
                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Output.Text = $"Output Hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Output port connection failed: {ex.Message}");
            }
        }

        private void DisconnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortService.CloseOutputPort();
                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Output.Text = "Çıkış Portu Kapalı";
                System.Diagnostics.Debug.WriteLine("Output port disconnected");
            }
            catch (Exception ex)
            {
                StatusText_Output.Text = $"Kapatma Hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Output port disconnect failed: {ex.Message}");
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