using copilot_deneme.ViewModels;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;

namespace copilot_deneme
{
    public class SerialPortService
    {
        // Mevcut input port
        private static SerialPort? _inputPort;

        // Yeni output port (HYI için)
        private static SerialPort? _outputPort;

        private static readonly List<byte> _binaryBuffer = new List<byte>();

        // HYI paket özellikleri
        private const int HYI_PACKET_SIZE = 78;
        private static readonly byte[] HYI_HEADER = { 0xFF, 0xFF, 0x54, 0x52 };

        // Normal telemetri paket özellikleri
        private const int NORMAL_PACKET_SIZE = 96;
        private static readonly byte[] NORMAL_HEADER = { 0xAA, 0xBB, 0xCC, 0xDD };

        #region Properties
        public static SerialPort? SerialPort
        {
            get => _inputPort;
            set => _inputPort = value;
        }

        public static ChartViewModel? ViewModel { get; set; }
        public static DispatcherQueue? Dispatcher { get; set; }
        #endregion

        #region Events
        public static event Action<string>? OnDataReceived;
        public static event Action<TelemetryUpdateData>? OnTelemetryDataUpdated;
        public static event Action<HYITelemetryData>? OnHYIPacketReceived;
        public static event Action<float, float, float>? OnRotationDataReceived;
        #endregion

        #region Public Methods
        public static void Initialize(string portName, int baudRate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Initializing SerialPort with port:{portName}, baudRate:{baudRate}");

                _inputPort?.Dispose();

                _inputPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                };

                System.Diagnostics.Debug.WriteLine("SerialPort initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing SerialPort: {ex.Message}");
                throw;
            }
        }

        public static void StartReading()
        {
            if (_inputPort == null)
                throw new InvalidOperationException("Serial port must be initialized before starting");

            try
            {
                _inputPort.DataReceived -= SerialPort_DataReceived;
                _inputPort.DataReceived += SerialPort_DataReceived;

                if (!_inputPort.IsOpen)
                {
                    _inputPort.Open();
                }

                System.Diagnostics.Debug.WriteLine($"StartReading called - Port open: {_inputPort.IsOpen}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting serial port reading: {ex.Message}");
                throw;
            }
        }

        public static void Write(byte[] data)
        {
            if (_inputPort == null || !_inputPort.IsOpen)
                throw new InvalidOperationException("Yazma iþlemi için port açýk deðil.");
            _inputPort.Write(data, 0, data.Length);
        }

        public static void StopReading()
        {
            try
            {
                if (_inputPort?.IsOpen == true)
                {
                    _inputPort.DataReceived -= SerialPort_DataReceived;
                    _inputPort.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping serial port: {ex.Message}");
            }
        }

        public static bool IsPortOpen() => _inputPort?.IsOpen == true;

        public static string GetPortInfo()
        {
            if (_inputPort == null)
                return "Port not initialized";

            return $"Port: {_inputPort.PortName}, BaudRate: {_inputPort.BaudRate}, IsOpen: {_inputPort.IsOpen}";
        }

        public static void Dispose()
        {
            try
            {
                StopReading();
                CloseOutputPort();
                _inputPort?.Dispose();
                _inputPort = null;
                _binaryBuffer.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing SerialPort: {ex.Message}");
            }
        }

        #region Output Port Methods
        public static void InitializeOutputPort(string portName, int baudRate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Initializing Output Port: {portName}, {baudRate}");

                _outputPort?.Dispose();

                _outputPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                };

                _outputPort.Open();
                System.Diagnostics.Debug.WriteLine("Output Port initialized and opened");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing output port: {ex.Message}");
                throw;
            }
        }

        public static void WriteToOutputPort(byte[] data)
        {
            if (_outputPort == null || !_outputPort.IsOpen)
                throw new InvalidOperationException("Output port açýk deðil.");

            try
            {
                _outputPort.Write(data, 0, data.Length);
                System.Diagnostics.Debug.WriteLine($"Data sent to output port: {data.Length} bytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to output port: {ex.Message}");
                throw;
            }
        }

        public static bool IsOutputPortOpen() => _outputPort?.IsOpen == true;

        public static void CloseOutputPort()
        {
            try
            {
                if (_outputPort?.IsOpen == true)
                {
                    _outputPort.Close();
                }
                _outputPort?.Dispose();
                _outputPort = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing output port: {ex.Message}");
            }
        }
        #endregion


        public static void UpdateChartsFromExternalData(float rocketAltitude, float payloadAltitude,
            float accelX,float accelY,float accelZ, float rocketSpeed, float payloadSpeed,
            float rocketTemp, float payloadTemp, float rocketPressure, float payloadPressure,
            float payloadHumidity, string source = "External",
            
            float rocketAccelX = 0, float rocketAccelY = 0)
        {
            Dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    if (ViewModel == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"{source}: ViewModel is null!");
                        return;
                    }

                    UpdateViewModelData(rocketAltitude, payloadAltitude,
                        rocketSpeed, payloadSpeed, rocketTemp, payloadTemp, rocketPressure,
                        payloadPressure, payloadHumidity, accelX, accelY, accelZ);

                    ViewModel.UpdateStatus($"{source} verisi: {DateTime.Now:HH:mm:ss}");

                    System.Diagnostics.Debug.WriteLine($"{source} TÜM VERÝLER chart'lara eklendi:");
                    System.Diagnostics.Debug.WriteLine($"  Roket: {rocketAltitude:F2}m, Payload: {payloadAltitude:F2}m");
                    
                    System.Diagnostics.Debug.WriteLine($"  Ývme X:{rocketAccelX:F2}, Y:{rocketAccelY:F2}, Z:{accelZ:F2}");
                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{source} chart güncelleme hatasý: {ex.Message}");
                }
            });
        }
        #endregion

        #region Private Methods
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_inputPort == null) return;

                int bytesToRead = _inputPort.BytesToRead;
                if (bytesToRead == 0) return;

                byte[] tempBuffer = new byte[bytesToRead];
                int bytesRead = _inputPort.Read(tempBuffer, 0, bytesToRead);

                // Debug: Log received bytes
                System.Diagnostics.Debug.WriteLine($"Received {bytesRead} bytes: {BitConverter.ToString(tempBuffer.Take(Math.Min(bytesRead, 20)).ToArray())}...");

                string dataAsString = System.Text.Encoding.UTF8.GetString(tempBuffer,0,bytesRead);
                OnDataReceived?.Invoke(dataAsString);

                _binaryBuffer.AddRange(tempBuffer.Take(bytesRead));
                // Debug: Log buffer size
                System.Diagnostics.Debug.WriteLine($"Buffer size: {_binaryBuffer.Count}");

                ProcessBinaryBuffer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SerialPort_DataReceived: {ex.Message}");
                OnDataReceived?.Invoke($"ERROR: {ex.Message}");
            }
        }

        private static void ProcessBinaryBuffer()
        {
            ProcessHYIPackets();
            ProcessNormalTelemetryPackets();
        }

        private static void ProcessHYIPackets()
        {
            while (_binaryBuffer.Count >= HYI_PACKET_SIZE)
            {
                int headerIndex = FindHeader(_binaryBuffer, HYI_HEADER);

                if (headerIndex == -1)
                {
                    if (_binaryBuffer.Count > HYI_HEADER.Length)
                        _binaryBuffer.RemoveAt(0);
                    else
                        break;
                    continue;
                }

                if (headerIndex > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"HYI Header found at index {headerIndex}, removing preceding bytes");
                    _binaryBuffer.RemoveRange(0, headerIndex);
                    continue;
                }

                if (_binaryBuffer.Count < HYI_PACKET_SIZE)
                    break;

                byte[] packet = _binaryBuffer.GetRange(0, HYI_PACKET_SIZE).ToArray();
                byte calculatedChecksum = CalculateChecksum(packet, 4, 71);
                byte receivedChecksum = packet[75];

                if (receivedChecksum == calculatedChecksum)
                {
                    System.Diagnostics.Debug.WriteLine("HYI packet checksum valid!");
                    var telemetryData = ParseHYIData(packet);

                  
                    if (IsOutputPortOpen())
                    {
                        try
                        {
                            WriteToOutputPort(packet);
                            System.Diagnostics.Debug.WriteLine($"HYI packet forwarded to output port - TeamID: {telemetryData.TeamId}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error forwarding HYI packet: {ex.Message}");
                        }
                    }

                    Dispatcher?.TryEnqueue(() =>
                    {
                        OnHYIPacketReceived?.Invoke(telemetryData);
                        System.Diagnostics.Debug.WriteLine($"HYI Packet Received - TeamID: {telemetryData.TeamId}, Counter: {telemetryData.PacketCounter}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("HYI Checksum hatasý!");
                }

                _binaryBuffer.RemoveRange(0, HYI_PACKET_SIZE);
            }
        }

        private static void ProcessNormalTelemetryPackets()
        {
            while (_binaryBuffer.Count >= NORMAL_PACKET_SIZE)
            {
                int headerIndex = FindHeader(_binaryBuffer, NORMAL_HEADER);

                if (headerIndex == -1)
                {
                    if (_binaryBuffer.Count > NORMAL_HEADER.Length)
                        _binaryBuffer.RemoveAt(0);
                    else
                        break;
                    continue;
                }
                    

                if (headerIndex > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Normal Header found at index {headerIndex}, removing preceding bytes");
                    _binaryBuffer.RemoveRange(0, headerIndex);
                    continue;
                }

                if (_binaryBuffer.Count < NORMAL_PACKET_SIZE)
                    break;

                byte[] packet = _binaryBuffer.GetRange(0, NORMAL_PACKET_SIZE).ToArray();
                var telemetryData = ParseNormalTelemetryData(packet);

                if (telemetryData != null)
                {
                    UpdateCharts(telemetryData);
                    OnTelemetryDataUpdated?.Invoke(telemetryData);
                }

                _binaryBuffer.RemoveRange(0, NORMAL_PACKET_SIZE);
            }
        }

        private static TelemetryUpdateData? ParseNormalTelemetryData(byte[] packet)
        {
            try
            {
                // First check if packet has correct header
                if (packet[0] != 0xAA || packet[1] != 0xBB || packet[2] != 0xCC || packet[3] != 0xDD)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid normal telemetry header!");
                    return null;
                }

                var telemetryData = new TelemetryUpdateData
                {
                    PacketCounter = packet[4],
                    RocketAltitude = BitConverter.ToSingle(packet, 5),
                    RocketLatitude = BitConverter.ToSingle(packet, 9),
                    RocketLongitude = BitConverter.ToSingle(packet, 13),
                    PayloadAltitude = BitConverter.ToSingle(packet, 17),
                    PayloadLatitude = BitConverter.ToSingle(packet, 21),
                    PayloadLongitude = BitConverter.ToSingle(packet, 25),
                    GyroX = BitConverter.ToSingle(packet, 29),
                    GyroY = BitConverter.ToSingle(packet, 33),
                    GyroZ = BitConverter.ToSingle(packet, 37),
                    AccelX = BitConverter.ToSingle(packet, 41),
                    AccelY = BitConverter.ToSingle(packet, 45),
                    AccelZ = BitConverter.ToSingle(packet, 49),
                    Angle = BitConverter.ToSingle(packet, 53),
                    RocketTemperature = BitConverter.ToSingle(packet, 57),
                    PayloadTemperature = BitConverter.ToSingle(packet, 61),
                    RocketPressure = BitConverter.ToSingle(packet, 65),
                    PayloadPressure = BitConverter.ToSingle(packet, 69),
                    PayloadHumidity = BitConverter.ToSingle(packet, 73),
                    RocketSpeed = BitConverter.ToSingle(packet, 77),
                    PayloadSpeed = BitConverter.ToSingle(packet, 81),
                    status = packet[85],
                    CRC = packet[86],
                    TeamID = 255,

                };

                byte calculatedCRC = CalculateSimpleCRC(packet, 4, 82);
                if (calculatedCRC != packet[86])
                {
                    System.Diagnostics.Debug.WriteLine("Normal telemetry CRC hatasý!");
                    //return null;
                }

                return telemetryData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing normal telemetry data: {ex.Message}");
                return null;
            }
        }

        private static int FindHeader(List<byte> buffer, byte[] header)
        {
            for (int i = 0; i <= buffer.Count - header.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < header.Length; j++)
                {
                    if (buffer[i + j] != header[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            return -1;
        }

        private static HYITelemetryData ParseHYIData(byte[] packet)
        {
            return new HYITelemetryData
            {
                TeamId = packet[4],
                PacketCounter = packet[5],
                Altitude = BitConverter.ToSingle(packet, 6),
                RocketGpsAltitude = BitConverter.ToSingle(packet, 10),
                RocketLatitude = BitConverter.ToSingle(packet, 14),
                RocketLongitude = BitConverter.ToSingle(packet, 18),
                PayloadGpsAltitude = BitConverter.ToSingle(packet, 22),
                PayloadLatitude = BitConverter.ToSingle(packet, 26),
                PayloadLongitude = BitConverter.ToSingle(packet, 30),
                StageGpsAltitude = BitConverter.ToSingle(packet, 34),
                StageLatitude = BitConverter.ToSingle(packet, 38),
                StageLongitude = BitConverter.ToSingle(packet, 42),
                GyroscopeX = BitConverter.ToSingle(packet, 46),
                GyroscopeY = BitConverter.ToSingle(packet, 50),
                GyroscopeZ = BitConverter.ToSingle(packet, 54),
                AccelerationX = BitConverter.ToSingle(packet, 58),
                AccelerationY = BitConverter.ToSingle(packet, 62),
                AccelerationZ = BitConverter.ToSingle(packet, 66),
                Angle = BitConverter.ToSingle(packet, 70),
                Status = packet[74],
                CRC = packet[75]
            };
        }

        private static byte CalculateChecksum(byte[] data, int offset, int length)
        {
            int sum = 0;
            for (int i = offset; i < offset + length; i++)
            {
                sum += data[i];
            }
            return (byte)(sum % 256);
        }

        private static byte CalculateSimpleCRC(byte[] data, int offset, int length)
        {
            byte crc = 0;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
            }
            return crc;
        }

        private static void UpdateCharts(TelemetryUpdateData telemetryData)
        {
            Dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    if (ViewModel == null)
                        return;

                    UpdateViewModelData(telemetryData.RocketAltitude, telemetryData.PayloadAltitude,
                        telemetryData.AccelZ, telemetryData.RocketSpeed, telemetryData.PayloadSpeed,
                        telemetryData.RocketTemperature, telemetryData.PayloadTemperature,
                        telemetryData.RocketPressure, telemetryData.PayloadPressure, telemetryData.PayloadHumidity,
                        telemetryData.AccelX, telemetryData.AccelY);

                    ViewModel.UpdateStatus($"Serial verisi: {DateTime.Now:HH:mm:ss} - Paket: {telemetryData.PacketCounter}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating charts: {ex.Message}");
                }
            });
        }

        private static void UpdateViewModelData(float rocketAltitude, float payloadAltitude,
            float accelZ, float accelX, float accelY, float rocketSpeed, float payloadSpeed,
            float rocketTemp, float payloadTemp, float rocketPressure, float payloadPressure,
            float payloadHumidity)
        {
            if (ViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateViewModelData: ViewModel is null!");
                return;
            }

            ViewModel.AddRocketAltitudeValue(rocketAltitude);
            ViewModel.addPayloadAltitudeValue(payloadAltitude);
            ViewModel.addRocketAccelXValue(accelX);
            ViewModel.addRocketAccelYValue(accelY);
            ViewModel.addRocketAccelZValue(accelZ);
            ViewModel.addRocketSpeedValue(rocketSpeed);
            ViewModel.addPayloadSpeedValue(payloadSpeed);
            ViewModel.addRocketTempValue(rocketTemp);
            ViewModel.addPayloadTempValue(payloadTemp);
            ViewModel.addRocketPressureValue(rocketPressure);
            ViewModel.addPayloadPressureValue(payloadPressure);
            ViewModel.addPayloadHumidityValue(payloadHumidity);

            System.Diagnostics.Debug.WriteLine($"ViewModel updated - Rocket Alt: {rocketAltitude:F2}, Payload Alt: {payloadAltitude:F2}");
        }
        #endregion



        public class TelemetryUpdateData
        {
            public float RocketAltitude { get; set; }
            public float RocketGpsAltitude { get; set; }
            public float RocketLatitude { get; set; }
            public float RocketLongitude { get; set; }
            public float PayloadAltitude { get; set; }
            public float PayloadLatitude { get; set; }
            public float PayloadLongitude { get; set; }
            public float GyroX { get; set; }
            public float GyroY { get; set; }
            public float GyroZ { get; set; }
            public float AccelX { get; set; }
            public float AccelY { get; set; }
            public float AccelZ { get; set; }
            public float Angle { get; set; }
            public float RocketSpeed { get; set; }
            public float PayloadSpeed { get; set; }
            public float RocketTemperature { get; set; }
            public float PayloadTemperature { get; set; }
            public float RocketPressure { get; set; }
            public float PayloadPressure { get; set; }
            public float PayloadHumidity { get; set; }
            public byte CRC { get; set; }
            public byte TeamID { get; set; }
            public byte status { get; set; }
            public byte PacketCounter { get; set; }
        }

        public class HYITelemetryData
        {
            public byte TeamId { get; set; }
            public byte PacketCounter { get; set; }
            public float Altitude { get; set; }
            public float RocketGpsAltitude { get; set; }
            public float RocketLatitude { get; set; }
            public float RocketLongitude { get; set; }
            public float PayloadGpsAltitude { get; set; }
            public float PayloadLatitude { get; set; }
            public float PayloadLongitude { get; set; }
            public float StageGpsAltitude { get; set; }
            public float StageLatitude { get; set; }
            public float StageLongitude { get; set; }
            public float GyroscopeX { get; set; }
            public float GyroscopeY { get; set; }
            public float GyroscopeZ { get; set; }
            public float AccelerationX { get; set; }
            public float AccelerationY { get; set; }
            public float AccelerationZ { get; set; }
            public float Angle { get; set; }
            public byte Status { get; set; }
            public byte CRC { get; set; }
        }
    }
}