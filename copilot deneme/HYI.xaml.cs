using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO.Ports;
using System.Linq;
using Windows.UI;

namespace copilot_deneme
{
    public sealed partial class HYI : Page
    {
        private DispatcherQueue _dispatcherQueue;
        private SerialPortServiceHYI _inputPortManager;
        private SerialPortServiceHYI _outputPortManager;
        private ushort _packetCounter = 0;

        public HYI()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _inputPortManager = new SerialPortServiceHYI(_dispatcherQueue);
            _outputPortManager = new SerialPortServiceHYI(_dispatcherQueue);

            _inputPortManager.PacketReceived += OnArduinoPacketReceived;

            this.Unloaded += (s, e) =>
            {
                _inputPortManager.Stop();
                _outputPortManager.Stop();
            };

            RefreshAvailablePorts();
        }

        private void OnArduinoPacketReceived(HYITelemetryData data)
        {
            HYITelemetryUpdate(data);
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshAvailablePorts();
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
        }

        private void ConnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Input.SelectedItem as string;
                if (string.IsNullOrEmpty(portName)) throw new InvalidOperationException("Giriþ için bir port seçin.");
                var baudRate = int.Parse((BaudRateComboBox_Input.SelectedItem as ComboBoxItem).Content.ToString());

                _inputPortManager.Initialize(portName, baudRate);
                _inputPortManager.Start();

                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Input.Text = $"Baðlandý: {portName}";
            }
            catch (Exception ex)
            {
                StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Input.Text = $"Hata: {ex.Message}";
            }
        }

        private void DisconnectInputPort_Click(object sender, RoutedEventArgs e)
        {
            _inputPortManager.Stop();
            StatusIndicator_Input.Fill = new SolidColorBrush(Colors.Red);
            StatusText_Input.Text = "Giriþ Portu Kapalý";
        }

        private void ConnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var portName = PortComboBox_Output.SelectedItem as string;
                if (string.IsNullOrEmpty(portName)) throw new InvalidOperationException("Çýkýþ için bir port seçin.");
                var baudRate = int.Parse((BaudRateComboBox_Output.SelectedItem as ComboBoxItem).Content.ToString());

                _outputPortManager.Initialize(portName, baudRate);
                _outputPortManager.Start();

                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.LightGreen);
                StatusText_Output.Text = $"Baðlandý: {portName}";
            }
            catch (Exception ex)
            {
                StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
                StatusText_Output.Text = $"Hata: {ex.Message}";
            }
        }

        private void DisconnectOutputPort_Click(object sender, RoutedEventArgs e)
        {
            _outputPortManager.Stop();
            StatusIndicator_Output.Fill = new SolidColorBrush(Colors.Red);
            StatusText_Output.Text = "Çýkýþ Portu Kapalý";
        }

        /// <summary>
        /// HYI'ye ait UI'yý gelen verilerle günceller.
        /// </summary>
        private void HYITelemetryUpdate(HYITelemetryData data)
        {
            if (data == null) return;

            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    takýmID.Text = data.TeamId.ToString();
                    sayac.Text = data.PacketCounter.ToString();
                    irtifa.Text = data.Altitude.ToString("F2");
                    RoketGPSirtifa.Text = data.RocketGpsAltitude.ToString("F2");
                    roketenlem.Text = data.RocketLatitude.ToString("F6");
                    roketboylam.Text = data.RocketLongitude.ToString("F6");
                    payloadGPSirtifa.Text = data.PayloadGpsAltitude.ToString("F2");
                    payloadGPSenlem.Text = data.PayloadLatitude.ToString("F6");
                    payloadGPSboylam.Text = data.PayloadLongitude.ToString("F6");
                    kademeGPSirtifa.Text = data.StageGpsAltitude.ToString("F2");
                    kademeGPSenlem.Text = data.StageLatitude.ToString("F6");
                    kademeGPSboylam.Text = data.StageLongitude.ToString("F6");
                    gyroX.Text = data.GyroscopeX.ToString("F2");
                    gyroY.Text = data.GyroscopeY.ToString("F2");
                    gyroZ.Text = data.GyroscopeZ.ToString("F2");
                    accelX.Text = data.AccelerationX.ToString("F2");
                    accelY.Text = data.AccelerationY.ToString("F2");
                    accelZ.Text = data.AccelerationZ.ToString("F2");
                    aci.Text = data.Angle.ToString("F2");
                    durum.Text = data.Status.ToString();
                    checksum.Text = data.CRC.ToString();
                }
                catch (Exception)
                {
                    // Opsiyonel: Hata loglamak istersen buraya yaz.
                }
            });
        }
    }
}
