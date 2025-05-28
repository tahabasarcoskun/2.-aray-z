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
                var viewModel = new ChartViewModel(); // Change ChartViewModels to ChartViewModel
            this.DataContext = viewModel;
            _viewModel = new ChartViewModel();
            SerialPortService.ViewModel = _viewModel; // Bu satýr çok önemli!
            SerialPortService.Dispatcher = DispatcherQueue.GetForCurrentThread();

            SerialPortService.ViewModel = viewModel; // This now matches the expected type
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
                if (SerialPortService.SerialPort != null && SerialPortService.SerialPort.IsOpen)
                    SerialPortService.SerialPort.Close();

                SerialPortService.SerialPort = new SerialPort(portName, baudRate);
                SerialPortService.SerialPort.Open();

                DispatcherQueue.TryEnqueue(() =>
                {
                    // All UI code here
                    StatusText.Text = "Port açýldý";
                });
            }
            catch (System.Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    // All UI code here
                    StatusText.Text = $"Error: {ex.Message}";
                });
            }
        }
    }
}