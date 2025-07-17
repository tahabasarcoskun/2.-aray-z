using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace copilot_deneme
{
    public class SerialPortServiceHYI
    {
        private SerialPort _serialPort;
        private readonly DispatcherQueue _dispatcher;
        private readonly List<byte> _buffer = new List<byte>();

        public event Action<HYITelemetryData> PacketReceived;

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;
        public string PortName => _serialPort?.PortName;

        private const int PACKET_SIZE_FROM_ARDUINO = 78;
        private readonly byte[] HEADER = { 0xFF, 0xFF, 0x54, 0x52 };

        public SerialPortServiceHYI(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Initialize(string portName, int baudRate)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                Stop();
            }
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void Start()
        {
            if (_serialPort != null && !_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void Stop()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
            }
            _buffer.Clear();
        }

        public void Write(byte[] data)
        {
            if (!IsOpen) throw new InvalidOperationException("Yazma işlemi için port açık değil.");
            _serialPort.Write(data, 0, data.Length);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                byte[] tempBuffer = new byte[bytesToRead];
                _serialPort.Read(tempBuffer, 0, bytesToRead);
                _buffer.AddRange(tempBuffer);
                ProcessBuffer();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Seri veri okuma hatası: {ex.Message}");
            }
        }

        private void ProcessBuffer()
        {
            while (_buffer.Count >= PACKET_SIZE_FROM_ARDUINO)
            {
                if (_buffer[0] == HEADER[0] && _buffer[1] == HEADER[1] &&
                    _buffer[2] == HEADER[2] && _buffer[3] == HEADER[3])
                {
                    byte[] packet = _buffer.GetRange(0, PACKET_SIZE_FROM_ARDUINO).ToArray();
                    byte calculatedChecksum = CalculateChecksum(packet, 4, 71);
                    byte receivedChecksum = packet[75];

                    if (receivedChecksum == calculatedChecksum)
                    {
                        var telemetryData = ParseHYIData(packet);
                        _dispatcher.TryEnqueue(() => PacketReceived?.Invoke(telemetryData));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Checksum hatası! Paket atlandı.");
                    }

                    _buffer.RemoveRange(0, PACKET_SIZE_FROM_ARDUINO);
                }
                else
                {
                    _buffer.RemoveAt(0);
                }
            }
        }

        private byte CalculateChecksum(byte[] data, int offset, int length)
        {
            int sum = 0;
            for (int i = offset; i < offset + length; i++)
            {
                sum += data[i];
            }
            return (byte)(sum % 256);
        }

        private HYITelemetryData ParseHYIData(byte[] packet)
        {
            HYITelemetryData data = new HYITelemetryData();
            data.TeamId = packet[4];
            data.PacketCounter = packet[5];
            data.Altitude = BitConverter.ToSingle(packet, 6);
            data.RocketGpsAltitude = BitConverter.ToSingle(packet, 10);
            data.RocketLatitude = BitConverter.ToSingle(packet, 14);
            data.RocketLongitude = BitConverter.ToSingle(packet, 18);
            data.PayloadGpsAltitude = BitConverter.ToSingle(packet, 22);
            data.PayloadLatitude = BitConverter.ToSingle(packet, 26);
            data.PayloadLongitude = BitConverter.ToSingle(packet, 30);
            data.StageGpsAltitude = BitConverter.ToSingle(packet, 34);
            data.StageLatitude = BitConverter.ToSingle(packet, 38);
            data.StageLongitude = BitConverter.ToSingle(packet, 42);
            data.GyroscopeX = BitConverter.ToSingle(packet, 46);
            data.GyroscopeY = BitConverter.ToSingle(packet, 50);
            data.GyroscopeZ = BitConverter.ToSingle(packet, 54);
            data.AccelerationX = BitConverter.ToSingle(packet, 58);
            data.AccelerationY = BitConverter.ToSingle(packet, 62);
            data.AccelerationZ = BitConverter.ToSingle(packet, 66);
            data.Angle = BitConverter.ToSingle(packet, 70);
            data.Status = packet[74];
            data.CRC = packet[75];
            return data;
        }
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
