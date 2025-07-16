// SerialPortManager.cs
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace copilot_deneme
{
    /// <summary>
    /// Tek bir seri port bağlantısını yönetmek için tasarlanmış yeniden kullanılabilir sınıf.
    /// Gelen binary veriyi tamponlar, doğrular ve tam bir paket alındığında olayı tetikler.
    /// </summary>
    public class SerialPortServiceHYI
    {
        private SerialPort _serialPort;
        private readonly DispatcherQueue _dispatcher;
        private readonly List<byte> _buffer = new List<byte>();

        /// <summary>
        /// Arduino'dan tam ve doğrulanmış bir veri paketi (payload) alındığında tetiklenir.
        /// </summary>
        public event Action<byte[]> PacketReceived;

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;
        public string PortName => _serialPort?.PortName;

        // Arduino'dan gelen beklenen paket yapısı
        private const int PAYLOAD_SIZE = 44; // 11 adet float * 4 byte/float
        private readonly byte[] HEADER = { 0xAA, 0x55 };
        private const int PACKET_SIZE_FROM_ARDUINO = 2 + PAYLOAD_SIZE + 1; // Header(2) + Payload(44) + Checksum(1) = 47 bytes

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
            // Tam bir paket için yeterli byte var mı diye sürekli kontrol et.
            while (_buffer.Count >= PACKET_SIZE_FROM_ARDUINO)
            {
                // Paketin başlangıç header'ını ara.
                if (_buffer[0] == HEADER[0] && _buffer[1] == HEADER[1])
                {
                    byte[] packet = _buffer.GetRange(0, PACKET_SIZE_FROM_ARDUINO).ToArray();
                    byte receivedChecksum = packet[PACKET_SIZE_FROM_ARDUINO - 1];
                    byte calculatedChecksum = CalculateChecksum(packet, 2, PAYLOAD_SIZE);

                    if (receivedChecksum == calculatedChecksum)
                    {
                        // Checksum doğruysa, payload'u ayıkla ve event'i tetikle.
                        byte[] payload = new byte[PAYLOAD_SIZE];
                        Array.Copy(packet, 2, payload, 0, PAYLOAD_SIZE);
                        _dispatcher.TryEnqueue(() => PacketReceived?.Invoke(payload));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Checksum hatası! Gelen paket atlanıyor.");
                    }
                    // İşlenen (veya hatalı) paketi buffer'dan kaldır.
                    _buffer.RemoveRange(0, PACKET_SIZE_FROM_ARDUINO);
                }
                else
                {
                    // Hatalı başlangıç byte'ını atla ve bir sonraki byte'tan devam et.
                    _buffer.RemoveAt(0);
                }
            }
        }

        private byte CalculateChecksum(byte[] data, int offset, int length)
        {
            byte checksum = 0;
            for (int i = 0; i < length; i++)
            {
                checksum ^= data[offset + i];
            }
            return checksum;
        }
    }
    public class HYITelemetryData
    {
        public byte TeamId { get; set; }
        public byte PacketCounter { get; set; }
        public byte Status { get; set; }
        public float Altitude { get; set; }
        public float RocketGpsAltitude { get; set; }
        public float RocketLatitude { get; set; }
        public float RocketLongitude { get; set; }
        public float PayloadGpsAltitude { get; set; }
        public float PayloadLatitude { get; set; }
        public float PayloadLongitude { get; set; }
        public float StageGpsAltitude { get; set; } // Sadece Zorlu Görev için
        public float StageLatitude { get; set; }    // Sadece Zorlu Görev için
        public float StageLongitude { get; set; }   // Sadece Zorlu Görev için
        public float GyroscopeX { get; set; }
        public float GyroscopeY { get; set; }
        public float GyroscopeZ { get; set; }
        public float AccelerationX { get; set; }
        public float AccelerationY { get; set; }
        public float AccelerationZ { get; set; }
        public float Angle { get; set; }
    }
}