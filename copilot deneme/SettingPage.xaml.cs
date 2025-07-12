using System.IO.Ports;
using copilot_deneme.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
            _viewModel = viewModel; // Ayný instance'ý kullan
            
            SerialPortService.ViewModel = _viewModel;
            SerialPortService.Dispatcher = _dispatcherQueue;
            
            // Ýlk yüklemede portlarý doldur
            RefreshAvailablePorts();
            BaudRateComboBox.SelectedIndex = 0; 
            
            // Sayfa yüklendiðinde durumu güncelle
            UpdateConnectionStatus();
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshAvailablePorts();
            
            // Kullanýcýya geri bildirim ver
            _dispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = "Portlar yenilendi";
                StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 144, 238, 144)); // LightGreen
            });
            
            System.Diagnostics.Debug.WriteLine("Ports refreshed");
        }
        
        private void RefreshAvailablePorts()
        {
            try
            {
                var availablePorts = SerialPort.GetPortNames();
                var currentSelection = PortComboBox.SelectedItem as string;
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    PortComboBox.ItemsSource = availablePorts;
                    
                    // Eðer önceden seçili bir port varsa ve hala mevcutsa, onu tekrar seç
                    if (!string.IsNullOrEmpty(currentSelection) && availablePorts.Contains(currentSelection))
                    {
                        PortComboBox.SelectedItem = currentSelection;
                    }
                    else if (availablePorts.Length > 0)
                    {
                        PortComboBox.SelectedIndex = 0; // Ýlk portu seç
                    }
                    
                    // Port sayýsýný göster
                    StatusText.Text = $"{availablePorts.Length} port bulundu";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 173, 216, 230)); // LightBlue
                });
                
                System.Diagnostics.Debug.WriteLine($"Found {availablePorts.Length} ports: {string.Join(", ", availablePorts)}");
            }
            catch (System.Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = $"Port tarama hatasý: {ex.Message}";
                        StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); // Red
                });
                
                System.Diagnostics.Debug.WriteLine($"Error refreshing ports: {ex.Message}");
            }
        }

        private void OpenPort_Click(object sender, RoutedEventArgs e)
        {
            var portName = PortComboBox.SelectedItem as string;
            var baudRateItem = BaudRateComboBox.SelectedItem as ComboBoxItem;
            var baudRate = int.Parse(baudRateItem?.Content.ToString() ?? "115200");

            if (string.IsNullOrEmpty(portName))
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = "Lütfen bir port seçin";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); // Orange
                });
                return;
            }

            try
            {
                // Önce mevcut baðlantýyý kapat
                if (SerialPortService.SerialPort != null && SerialPortService.SerialPort.IsOpen)
                {
                    SerialPortService.StopReading();
                }

                // Yeni port yapýlandýrmasý
                SerialPortService.Initialize(portName, baudRate);
                SerialPortService.StartReading();

                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = $"Baðlandý: {portName} ({baudRate} baud)";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 144, 238, 144)); // LightGreen
                    UpdateConnectionStatus();
                });
                
                System.Diagnostics.Debug.WriteLine($"Port opened successfully: {portName} at {baudRate} baud");
            }
            catch (System.Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = $"Baðlantý hatasý: {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); // Red
                    UpdateConnectionStatus();
                });
                System.Diagnostics.Debug.WriteLine($"Error opening port: {ex.Message}");
            }
        }
        
        private void ClosePort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortService.StopReading();
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = "Baðlantý kapatýldý";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); // Orange
                    UpdateConnectionStatus();
                });
                
                System.Diagnostics.Debug.WriteLine("Port closed successfully");
            }
            catch (System.Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusText.Text = $"Kapatma hatasý: {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); // Red
                });
                System.Diagnostics.Debug.WriteLine($"Error closing port: {ex.Message}");
            }
        }
        
        private void UpdateConnectionStatus()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                bool isConnected = SerialPortService.IsPortOpen();
                
                if (isConnected)
                {
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 144, 238, 144)); // LightGreen
                    if (StatusText.Text == "Port baðlantýsý kapalý")
                    {
                        StatusText.Text = $"Baðlý: {SerialPortService.SerialPort?.PortName}";
                        StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 144, 238, 144)); // LightGreen
                    }
                }
                else
                {
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); // Red
                    if (!StatusText.Text.Contains("hatasý") && !StatusText.Text.Contains("yenilendi"))
                    {
                        StatusText.Text = "Port baðlantýsý kapalý";
                        StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)); // Gray
                    }
                }
            });
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Sayfa her açýldýðýnda durum güncelle
            UpdateConnectionStatus();
            RefreshAvailablePorts();
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            // Cleanup if needed
        }
    }
}