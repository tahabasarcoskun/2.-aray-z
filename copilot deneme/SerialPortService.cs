using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using Microsoft.UI.Dispatching;
using copilot_deneme.ViewModels;
using System.Globalization;

namespace copilot_deneme
{
    public static class SerialPortService
    {
        private static SerialPort _serialPort;
        private static string _buffer = string.Empty;
        
        public static SerialPort SerialPort             
        {
            get => _serialPort;
            set => _serialPort = value;
        }

        public static ChartViewModel ViewModel { get; set; }
        public static DispatcherQueue Dispatcher { get; set; }
        
        // Yeni event handler - ham veri için
        public static event Action<string> OnDataReceived;

        public static void Initialize(string portName, int baudRate)
        {
            System.Diagnostics.Debug.WriteLine($"Initializing SerialPort with port:{portName}, baudRate:{baudRate}");
            SerialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };
            System.Diagnostics.Debug.WriteLine("SerialPort initialized");
        }

        public static void StartReading()
        {
            if (SerialPort == null)
                throw new InvalidOperationException("Serial port must be initialized before starting");

            // Event handler'ý her zaman ekle (duplicate kontrolü yap)
            SerialPort.DataReceived -= SerialPort_DataReceived; // Önce kaldýr
            SerialPort.DataReceived += SerialPort_DataReceived; // Sonra ekle
            
            if (!SerialPort.IsOpen)
            {
                SerialPort.Open();
            }
            
            System.Diagnostics.Debug.WriteLine($"StartReading called - Port open: {SerialPort.IsOpen}, Event handler added");
        }

        public static void StopReading()
        {
            if (SerialPort?.IsOpen == true)
            {
                SerialPort.DataReceived -= SerialPort_DataReceived;
                SerialPort.Close();
            }
        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Mevcut buffer'daki tüm veriyi oku
                string availableData = SerialPort.ReadExisting();
                System.Diagnostics.Debug.WriteLine($"Raw Serial Data: '{availableData}'");

                // Ham veriyi event ile gönder
                OnDataReceived?.Invoke(availableData);

                // Chart verilerini iþle (comma-separated format)
                ProcessArduinoData(availableData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SerialPort_DataReceived: {ex.Message}");
                OnDataReceived?.Invoke($"ERROR: {ex.Message}");
            }
        }

        private static void ProcessArduinoData(string receivedData)
        {
            try
            {
                _buffer += receivedData;

                // Satýrlarý ayýr
                string[] lines = _buffer.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Son satýr eksik olabilir, onu buffer'da býrak
                if (!_buffer.EndsWith('\n') && !_buffer.EndsWith('\r') && lines.Length > 0)
                {
                    _buffer = lines[lines.Length - 1];
                    Array.Resize(ref lines, lines.Length - 1);
                }
                else
                {
                    _buffer = string.Empty;
                }

                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ProcessDataLine(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProcessArduinoData: {ex.Message}");
            }
        }

        private static void ProcessDataLine(string data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing line: '{data}'");
                
                // Önce key:value formatýný kontrol et
                if (data.Contains(':'))
                {
                    ProcessKeyValueData(data);
                    return;
                }

                // Comma-separated formatýný iþle
                string[] parcalar = data.Split(',');
                
                if (parcalar.Length < 15)
                {
                    System.Diagnostics.Debug.WriteLine($"Beklenen en az 15 veri, alýnan: {parcalar.Length}");
                    return;
                }

                // InvariantCulture kullan - nokta (.) ondalýk ayýrýcý olarak
                var culture = CultureInfo.InvariantCulture;

                // Gelen verileri parse et
                float irtifa = TryParseFloat(parcalar[0], culture, 0);
                float roketGpsIrtifa = TryParseFloat(parcalar[1], culture, 0);
                float roketGpsEnlem = TryParseFloat(parcalar[2], culture, 0);
                float roketGpsBoylam = TryParseFloat(parcalar[3], culture, 0);
                float payloadGpsIrtifa = TryParseFloat(parcalar[4], culture, 0);
                float payloadGpsEnlem = TryParseFloat(parcalar[5], culture, 0);
                float payloadGpsBoylam = TryParseFloat(parcalar[6], culture, 0);
                float jiroskopX = TryParseFloat(parcalar[7], culture, 0);
                float jiroskopY = TryParseFloat(parcalar[8], culture, 0);
                float jiroskopZ = TryParseFloat(parcalar[9], culture, 0);
                float ivmeX = TryParseFloat(parcalar[10], culture, 0);
                float ivmeY = TryParseFloat(parcalar[11], culture, 0);
                float ivmeZ = TryParseFloat(parcalar[12], culture, 0);
                float aci = TryParseFloat(parcalar[13], culture, 0);
                byte durum = (byte)TryParseFloat(parcalar[14], culture, 0);

                System.Diagnostics.Debug.WriteLine($"Successfully parsed - Irtifa: {irtifa}, IvmeZ: {ivmeZ}");

                // Chart'lara veri ekle
                UpdateCharts(irtifa, roketGpsIrtifa, payloadGpsIrtifa, ivmeZ, jiroskopX, jiroskopY, jiroskopZ, ivmeX, ivmeY, aci);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Arduino data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Raw data was: '{data}'");
            }
        }

        private static void ProcessKeyValueData(string data)
        {
            var parts = data.Split(':', 2);
            if (parts.Length != 2)
                return;

            string key = parts[0].Trim();
            string valueStr = parts[1].Trim();

            if (!float.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedValue))
                return;

            Dispatcher?.TryEnqueue(() =>
            {
                switch (key)
                {
                    case "rocketAltitude":
                        ViewModel?.AddRocketAltitudeValue(parsedValue);
                        break;
                    case "payloadAltitude":
                        ViewModel?.addPayloadAltitudeValue(parsedValue);
                        break;
                    case "rocketAccelZ":
                        ViewModel?.addRocketAccelZValue(parsedValue);
                        break;
                    case "payloadAccelZ":
                        ViewModel?.addPayloadAccelZValue(parsedValue);
                        break;
                    case "rocketSpeed":
                        ViewModel?.addRocketSpeedValue(parsedValue);
                        break;
                    case "payloadSpeed":
                        ViewModel?.addPayloadSpeedValue(parsedValue);
                        break;
                    case "rocketTemperature":
                        ViewModel?.addRocketTempValue(parsedValue);
                        break;
                    case "payloadTemperature":
                        ViewModel?.addPayloadTempValue(parsedValue);
                        break;
                    case "rocketPressure":
                        ViewModel?.addRocketPressureValue(parsedValue);
                        break;
                    case "payloadPressure":
                        ViewModel?.addPayloadPressureValue(parsedValue);
                        break;
                    case "payloadHumidity":
                        ViewModel?.addPayloadHumidityValue(parsedValue);
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"Bilinmeyen anahtar alýndý: {key}");
                        break;
                }
            });
        }

        private static void UpdateCharts(float irtifa, float roketGpsIrtifa, float payloadGpsIrtifa, 
            float ivmeZ, float jiroskopX, float jiroskopY, float jiroskopZ, 
            float ivmeX, float ivmeY, float aci)
        {
            Dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    if (ViewModel != null)
                    {
                        // Ýrtifa verileri
                        ViewModel.AddRocketAltitudeValue(irtifa);
                        ViewModel.addPayloadAltitudeValue(payloadGpsIrtifa);
                        
                        // Ývme verileri
                        ViewModel.addRocketAccelZValue(ivmeZ);
                        ViewModel.addPayloadAccelZValue(ivmeZ * 0.9f); // Payload için biraz farklý deðer
                        
                        // Hýz verileri (türetilmiþ)
                        float roketHiz = Math.Abs(ivmeZ * 10); // Basit hýz hesabý
                        float payloadHiz = Math.Abs(ivmeZ * 9); 
                        ViewModel.addRocketSpeedValue(roketHiz);
                        ViewModel.addPayloadSpeedValue(payloadHiz);
                        
                        // Sýcaklýk verileri (simüle edilmiþ)
                        float roketTemp = 20 + (irtifa * 0.01f); 
                        float payloadTemp = 22 + (payloadGpsIrtifa * 0.01f);
                        ViewModel.addRocketTempValue(roketTemp);
                        ViewModel.addPayloadTempValue(payloadTemp);
                        
                        // Basýnç verileri (simüle edilmiþ)
                        float roketBasinc = 1013 - (irtifa * 0.1f);
                        float payloadBasinc = 1013 - (payloadGpsIrtifa * 0.1f);
                        ViewModel.addRocketPressureValue(roketBasinc);
                        ViewModel.addPayloadPressureValue(payloadBasinc);
                        
                        // Nem verisi (simüle edilmiþ)
                        float payloadNem = 50 + (payloadGpsIrtifa * 0.02f);
                        ViewModel.addPayloadHumidityValue(payloadNem);
                        
                        ViewModel.UpdateStatus($"Chart veri güncellendi: {DateTime.Now:HH:mm:ss}");
                        
                        System.Diagnostics.Debug.WriteLine($"Chart data added - Altitude: {irtifa}, Accel: {ivmeZ}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ViewModel is null!");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating charts: {ex.Message}");
                }
            });
        }

        private static float TryParseFloat(string value, CultureInfo culture, float defaultValue)
        {
            if (float.TryParse(value?.Trim(), culture, out float result))
                return result;
            return defaultValue;
        }

        public static bool IsPortOpen()
        {
            return SerialPort?.IsOpen == true;
        }

        public static string GetPortInfo()
        {
            if (SerialPort == null)
                return "Port not initialized";

            return $"Port: {SerialPort.PortName}, BaudRate: {SerialPort.BaudRate}, IsOpen: {SerialPort.IsOpen}";
        }

        public static void Dispose()
        {
            try
            {
                StopReading();
                SerialPort?.Dispose();
                SerialPort = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing SerialPort: {ex.Message}");
            }
        }

        // Test için public metod
        public static void TriggerTestData(string testData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Triggering test data: {testData}");
                OnDataReceived?.Invoke(testData);
                ProcessArduinoData(testData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error triggering test data: {ex.Message}");
            }
        }
    }
}