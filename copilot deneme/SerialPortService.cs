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

                // Satýrlarý ayýr
                string[] lines = availableData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ProcessArduinoData(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SerialPort_DataReceived: {ex.Message}");
                OnDataReceived?.Invoke($"ERROR: {ex.Message}");
            }
        }

        private static void ProcessArduinoData(string data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing line: '{data}'");
                
                string[] parcalar = data.Split(',');
                
                if (parcalar.Length != 15)
                {
                    System.Diagnostics.Debug.WriteLine($"Beklenen 15 veri, alýnan: {parcalar.Length}");
                    System.Diagnostics.Debug.WriteLine($"Data parts: [{string.Join("] [", parcalar)}]");
                    
                    // Eksik veri geldiðinde test verisi oluþtur
                    if (parcalar.Length >= 5)
                    {
                        AddTestDataToCharts(parcalar);
                    }
                    return;
                }

                // InvariantCulture kullan - nokta (.) ondalýk ayýrýcý olarak
                var culture = CultureInfo.InvariantCulture;

                float irtifa = float.Parse(parcalar[0], culture);
                float roketGpsIrtifa = float.Parse(parcalar[1], culture);
                float roketGpsEnlem = float.Parse(parcalar[2], culture);
                float roketGpsBoylam = float.Parse(parcalar[3], culture);
                float payloadGpsIrtifa = float.Parse(parcalar[4], culture);
                float payloadGpsEnlem = float.Parse(parcalar[5], culture);
                float payloadGpsBoylam = float.Parse(parcalar[6], culture);
                float jiroskopX = float.Parse(parcalar[7], culture);
                float jiroskopY = float.Parse(parcalar[8], culture);
                float jiroskopZ = float.Parse(parcalar[9], culture);
                float ivmeX = float.Parse(parcalar[10], culture);
                float ivmeY = float.Parse(parcalar[11], culture);
                float ivmeZ = float.Parse(parcalar[12], culture);
                float aci = float.Parse(parcalar[13], culture);
                byte durum = byte.Parse(parcalar[14], culture);

                System.Diagnostics.Debug.WriteLine($"Successfully parsed - Irtifa: {irtifa}, IvmeX: {ivmeX}, IvmeY: {ivmeY}, Durum: {durum}");

                // Chart'lara veri ekle
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
                            
                            ViewModel.UpdateStatus($"Veri güncellendi: {DateTime.Now:HH:mm:ss}");
                            
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Arduino data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Raw data was: '{data}'");
                OnDataReceived?.Invoke($"PARSE ERROR: {ex.Message} - Data: {data}");
            }
        }

        private static void AddTestDataToCharts(string[] parcalar)
        {
            try
            {
                var culture = CultureInfo.InvariantCulture;
                
                // Mevcut verilerden test verisi oluþtur
                float irtifa = parcalar.Length > 0 ? TryParseFloat(parcalar[0], culture, 100) : 100;
                float ivmeZ = parcalar.Length > 10 ? TryParseFloat(parcalar[10], culture, -1.0f) : -1.0f;
                
                Dispatcher?.TryEnqueue(() =>
                {
                    try
                    {
                        if (ViewModel != null)
                        {
                            // Test verileri ekle
                            ViewModel.AddRocketAltitudeValue(irtifa + (float)(new Random().NextDouble() * 10));
                            ViewModel.addPayloadAltitudeValue(irtifa + (float)(new Random().NextDouble() * 8));
                            
                            ViewModel.addRocketAccelZValue(ivmeZ + (float)(new Random().NextDouble() * 0.5));
                            ViewModel.addPayloadAccelZValue(ivmeZ + (float)(new Random().NextDouble() * 0.3));
                            
                            ViewModel.addRocketSpeedValue((float)(new Random().NextDouble() * 50));
                            ViewModel.addPayloadSpeedValue((float)(new Random().NextDouble() * 45));
                            
                            ViewModel.addRocketTempValue(20 + (float)(new Random().NextDouble() * 15));
                            ViewModel.addPayloadTempValue(22 + (float)(new Random().NextDouble() * 12));
                            
                            ViewModel.addRocketPressureValue(1000 + (float)(new Random().NextDouble() * 100));
                            ViewModel.addPayloadPressureValue(990 + (float)(new Random().NextDouble() * 120));
                            
                            ViewModel.addPayloadHumidityValue(40 + (float)(new Random().NextDouble() * 30));
                            
                            ViewModel.UpdateStatus($"Test verisi eklendi: {DateTime.Now:HH:mm:ss}");
                            
                            System.Diagnostics.Debug.WriteLine("Test chart data added");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding test chart data: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating test chart data: {ex.Message}");
            }
        }

        private static float TryParseFloat(string value, CultureInfo culture, float defaultValue)
        {
            if (float.TryParse(value, culture, out float result))
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
                
                // Test chart verisi de ekle
                Dispatcher?.TryEnqueue(() =>
                {
                    if (ViewModel != null)
                    {
                        var random = new Random();
                        ViewModel.AddRocketAltitudeValue(1000 + (float)(random.NextDouble() * 500));
                        ViewModel.addPayloadAltitudeValue(980 + (float)(random.NextDouble() * 450));
                        ViewModel.addRocketAccelZValue(-2 + (float)(random.NextDouble() * 4));
                        ViewModel.addPayloadAccelZValue(-1.8f + (float)(random.NextDouble() * 3.6f));
                        ViewModel.UpdateStatus("Test data triggered");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error triggering test data: {ex.Message}");
            }
        }
    }
}