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
        private static SerialPort? _inputPort;
        private static SerialPort? _outputPort;

        private static readonly List<byte> _binaryBuffer = new List<byte>();

        // HYI paket özellikleri
        private const int HYI_PACKET_SIZE = 78;
        private static readonly byte[] HYI_HEADER = { 0xFF, 0xFF, 0x54, 0x52 };

        //Roket telemetri paketi
        private const int ROCKET_PACKET_SIZE = 64;
        private static readonly byte[] ROCKET_HEADER = { 0xAB, 0xBC, 0x12, 0x13 };

        //payload telemetri paketi
        private const int PAYLOAD_PACKET_SIZE = 34;
        private static readonly byte[] PAYLOAD_HEADER = { 0xCD, 0XDF, 0x14, 0x15 };


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
        public static event Action<PayloadTelemetryData>? OnPayloadDataUpdated;
        public static event Action<RocketTelemetryData>? OnRocketDataUpdated;
        public static event Action<HYITelemetryData>? OnHYIPacketReceived;
        public static event Action<float, float, float>? OnRotationDataReceived;
        public static event Action<RocketTelemetryData, PayloadTelemetryData>? OnTelemetryDataUpdated;
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
                throw new InvalidOperationException("yazma işlemi için port açık değil");
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
                throw new InvalidOperationException("Output port açık değil.");

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

                    System.Diagnostics.Debug.WriteLine($"{source} tüm veriler chart'lara eklendi:");
                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{source} chart g�ncelleme hatas�: {ex.Message}");
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
            ProcessPayloadTelemetryPackets();
            ProcessRocketTelemetryPackets();
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
                    System.Diagnostics.Debug.WriteLine("HYI Checksum hatas�!");
                }

                _binaryBuffer.RemoveRange(0, HYI_PACKET_SIZE);
            }
        }

        private static void ProcessPayloadTelemetryPackets()
        {
            while (_binaryBuffer.Count >= PAYLOAD_HEADER.Length)
            {
                int headerIndex = FindHeader(_binaryBuffer, PAYLOAD_HEADER);

                if (headerIndex == -1)
                {
                    if (_binaryBuffer.Count > PAYLOAD_HEADER.Length)
                        _binaryBuffer.RemoveAt(0);
                    else
                        break;
                    continue;
                }
                    

                if (headerIndex > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Payload Header found at index {headerIndex}, removing preceding bytes");
                    _binaryBuffer.RemoveRange(0, headerIndex);
                    continue;
                }

                if (_binaryBuffer.Count < PAYLOAD_PACKET_SIZE)
                    break;

                byte[] packet = _binaryBuffer.GetRange(0, PAYLOAD_PACKET_SIZE).ToArray();
                var telemetryData = ParsePayloadData(packet);

                if (telemetryData != null)
                {
                    UpdateCharts(null, telemetryData);
                    OnPayloadDataUpdated?.Invoke(telemetryData);
                }

                _binaryBuffer.RemoveRange(0, PAYLOAD_PACKET_SIZE);
            }
        }

        private static void ProcessRocketTelemetryPackets()
        {
            while (_binaryBuffer.Count >= ROCKET_HEADER.Length)
            {
                int headerIndex = FindHeader(_binaryBuffer, ROCKET_HEADER);

                if (headerIndex == -1)
                {
                    if (_binaryBuffer.Count > ROCKET_HEADER.Length)
                        _binaryBuffer.RemoveAt(0);
                    else
                        break;
                    continue;
                }


                if (headerIndex > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Rocket Header found at index {headerIndex}, removing preceding bytes");
                    _binaryBuffer.RemoveRange(0, headerIndex);
                    continue;
                }

                if (_binaryBuffer.Count < ROCKET_PACKET_SIZE)
                    break;

                byte[] packet = _binaryBuffer.GetRange(0, ROCKET_PACKET_SIZE).ToArray();
                var telemetryData = ParseRocketData(packet);

                if (telemetryData != null)
                {
                    UpdateCharts(telemetryData, null);
                    OnRocketDataUpdated?.Invoke(telemetryData);
                }

                _binaryBuffer.RemoveRange(0, ROCKET_PACKET_SIZE);
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
        private static RocketTelemetryData? ParseRocketData(byte[] packet)
        {
            try
            {
                // First check if packet has correct header
                if (packet[0] != 0xAB || packet[1] != 0xBC || packet[2] != 0x12 || packet[3] != 0x13)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid rocket telemetry header!");
                    return null;
                }

                var rocketTelemetryData = new RocketTelemetryData
                {
                    PacketCounter = packet[4],
                    RocketAltitude = BitConverter.ToSingle(packet, 5),
                    RocketGpsAltitude = BitConverter.ToSingle(packet, 9),
                    RocketLatitude = BitConverter.ToSingle(packet, 13),
                    RocketLongitude = BitConverter.ToSingle(packet, 17),
                    GyroX = BitConverter.ToSingle(packet, 21),
                    GyroY = BitConverter.ToSingle(packet, 25),
                    GyroZ = BitConverter.ToSingle(packet, 29),
                    AccelX = BitConverter.ToSingle(packet, 33),
                    AccelY = BitConverter.ToSingle(packet, 37),
                    AccelZ = BitConverter.ToSingle(packet, 41),
                    Angle = BitConverter.ToSingle(packet, 45),
                    RocketTemperature = BitConverter.ToSingle(packet, 49),
                    RocketPressure = BitConverter.ToSingle(packet, 53),
                    RocketSpeed = BitConverter.ToSingle(packet, 57),
                    status = packet[61],
                    CRC = packet[62],
                    TeamID = 255,
                };

                byte calculatedCRC = CalculateSimpleCRC(packet, 4, 57);
                if (calculatedCRC != packet[62])
                {
                    System.Diagnostics.Debug.WriteLine("rocket telemetry CRC error!");
                    // CRC hatası olsa bile veriyi döndür (comment'e göre)
                }

                return rocketTelemetryData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing rocket telemetry data: {ex.Message}");
                return null;
            }
        }

        private static PayloadTelemetryData? ParsePayloadData(byte[] packet)
        {
            try
            {
                // First check if packet has correct header
                if (packet[0] != 0xCD || packet[1] != 0xDF || packet[2] != 0x14 || packet[3] != 0x15)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid payload telemetry header!");
                    return null;
                }

                var payloadTelemetryData = new PayloadTelemetryData
                {
                    PacketCounter = packet[4],
                    PayloadAltitude = BitConverter.ToSingle(packet, 5),
                    PayloadGpsAltitude = BitConverter.ToSingle(packet, 9),
                    PayloadLatitude = BitConverter.ToSingle(packet, 13),
                    PayloadLongitude = BitConverter.ToSingle(packet, 17),
                    PayloadSpeed = BitConverter.ToSingle(packet, 21),
                    PayloadTemperature = BitConverter.ToSingle(packet, 25),
                    PayloadPressure = BitConverter.ToSingle(packet, 29),
                    PayloadHumidity = BitConverter.ToSingle(packet, 33),
                    CRC = packet[37],
                };

                byte calculatedCRC = CalculateSimpleCRC(packet, 4, 33);
                if (calculatedCRC != packet[37])
                {
                    System.Diagnostics.Debug.WriteLine("payload telemetry CRC error!");
                    // CRC hatası olsa bile veriyi döndür (comment'e göre)
                }

                return payloadTelemetryData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing payload telemetry data: {ex.Message}");
                return null;
            }
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

        private static void UpdateCharts(RocketTelemetryData? rocketTelemetry, PayloadTelemetryData? payloadTelemetry)
        {
            Dispatcher?.TryEnqueue(() =>
            {
                try
                {
                    if (ViewModel == null)
                        return;

                    UpdateViewModelData(rocketTelemetry.RocketAltitude, rocketTelemetry.RocketSpeed,
                        rocketTelemetry.AccelX, rocketTelemetry.AccelY, rocketTelemetry.AccelZ,
                        rocketTelemetry.RocketTemperature, rocketTelemetry.RocketPressure
,                       payloadTelemetry.PayloadSpeed,payloadTelemetry.PayloadAltitude,payloadTelemetry.PayloadHumidity,
                        payloadTelemetry.PayloadTemperature, payloadTelemetry.PayloadPressure

                        );

                    ViewModel.UpdateStatus($"Serial verisi: {DateTime.Now:HH:mm:ss} - Paket: {rocketTelemetry.PacketCounter}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating charts: {ex.Message}");
                }
            });
        }

        private static void UpdateViewModelData(float rocketAltitude, float payloadAltitude,
            float rocketSpeed, float payloadSpeed, float rocketTemp, float payloadTemp, 
            float rocketPressure, float payloadPressure, float payloadHumidity, 
            float accelX, float accelY, float accelZ)
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



        public class RocketTelemetryData
        {
            public float RocketAltitude { get; set; }
            public float RocketGpsAltitude { get; set; }
            public float RocketLatitude { get; set; }
            public float RocketLongitude { get; set; }
            public float GyroX { get; set; }
            public float GyroY { get; set; }
            public float GyroZ { get; set; }
            public float AccelX { get; set; }
            public float AccelY { get; set; }
            public float AccelZ { get; set; }
            public float Angle { get; set; }
            public float RocketSpeed { get; set; }
            public float RocketTemperature { get; set; }  
            public float RocketPressure { get; set; }
            public byte CRC { get; set; }
            public byte TeamID { get; set; }
            public byte status { get; set; }
            public byte PacketCounter { get; set; }
        }
            
        public class PayloadTelemetryData
        {
            public float PayloadGpsAltitude { get; set; }
            public float PayloadAltitude { get; set; }
            public float PayloadLatitude { get; set; }
            public float PayloadLongitude { get; set; }
            public float PayloadSpeed { get; set; }
            public float PayloadTemperature { get; set; }
            public float PayloadPressure { get; set; }
            public float PayloadHumidity { get; set; }
            public byte CRC { get; set; }
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