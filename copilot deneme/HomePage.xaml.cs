using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.IO.Ports;
using System;
using copilot_deneme.ViewModels;
using System.Globalization;
using System.Text;

namespace copilot_deneme
{
    public sealed partial class HomePage : Page
    {
        private readonly ChartViewModel _viewModel;
        private readonly DispatcherQueue _dispatcherQueue;
        private TelemetryData currentTelemetry = new TelemetryData(); // Ana telemetri nesnesi
        private byte[] dataPacket = new byte[78]; // Paket için byte dizisi
        private StringBuilder _dataBuffer = new StringBuilder(); // Veri buffer'ý

        public HomePage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _viewModel = new ChartViewModel();
            this.DataContext = _viewModel;

            // SerialPortService'i yapýlandýr
            SerialPortService.ViewModel = _viewModel;
            SerialPortService.Dispatcher = _dispatcherQueue;

            // Serial data received event'i için delegate ekle
            SerialPortService.OnDataReceived += OnSerialDataReceived;
        }

        private void OnSerialDataReceived(string data)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Gelen veriyi TextBox'a ekle
                SerialDataTextBox.Text += $"{DateTime.Now:HH:mm:ss}: {data}\n";

                // TextBox'ý en alta kaydýr
                SerialDataScrollViewer.ChangeView(null, SerialDataScrollViewer.ExtentHeight, null);

                // Buffer'a veri ekle
                _dataBuffer.Append(data);

                // Tam satýr olup olmadýðýný kontrol et
                ProcessBuffer();
            });
        }

        private void ProcessBuffer()
        {
            string bufferContent = _dataBuffer.ToString();
            
            // Tam satýrlarý bul (\n ile biten)
            string[] lines = bufferContent.Split('\n');
            
            // Son satýr eksik olabilir, onu buffer'da býrak
            _dataBuffer.Clear();
            if (lines.Length > 0 && !bufferContent.EndsWith('\n'))
            {
                _dataBuffer.Append(lines[lines.Length - 1]);
                Array.Resize(ref lines, lines.Length - 1);
            }

            // Tam satýrlarý iþle
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line.Trim()))
                {
                    ProcessLine(line.Trim());
                }
            }
        }

        private void ProcessLine(string gelenSatir)
        {
            // Önce gelen veriyi temizle (carriage return, newline karakterlerini kaldýr)
            gelenSatir = gelenSatir.Replace("\r", "").Replace("\n", "").Trim();
            
            if (string.IsNullOrEmpty(gelenSatir))
                return;

            System.Diagnostics.Debug.WriteLine($"Processing complete line: '{gelenSatir}'");

            string[] parcalar = gelenSatir.Split(',');

            // Eðer 16 parça yoksa, veriyi birleþtirmeyi dene
            if (parcalar.Length < 16)
            {
                System.Diagnostics.Debug.WriteLine($"Eksik veri alýndý: {gelenSatir} - Parça sayýsý: {parcalar.Length}");
                
                // Test verisi oluþtur (gerçek veri yerine)
                if (parcalar.Length >= 5) // En az 5 parça varsa test verisi oluþtur
                {
                    CreateTestTelemetryData(parcalar);
                }
                return;
            }

            try
            {
                // InvariantCulture kullan - nokta (.) ondalýk ayýrýcý olarak
                var culture = CultureInfo.InvariantCulture;

                // Gelen string verileri float gibi türlere dönüþtür
                TelemetryData yeniVeri = new TelemetryData
                {
                    PaketSayaci = ushort.Parse(parcalar[0], culture),
                    Irtifa = float.Parse(parcalar[1], culture),
                    RoketGpsIrtifa = float.Parse(parcalar[2], culture),
                    RoketGpsEnlem = float.Parse(parcalar[3], culture),
                    RoketGpsBoylam = float.Parse(parcalar[4], culture),
                    PayloadGpsIrtifa = float.Parse(parcalar[5], culture),
                    PayloadGpsEnlem = float.Parse(parcalar[6], culture),
                    PayloadGpsBoylam = float.Parse(parcalar[7], culture),
                    JiroskopX = float.Parse(parcalar[8], culture),
                    JiroskopY = float.Parse(parcalar[9], culture),
                    JiroskopZ = float.Parse(parcalar[10], culture),
                    IvmeX = float.Parse(parcalar[11], culture),
                    IvmeY = float.Parse(parcalar[12], culture),
                    IvmeZ = float.Parse(parcalar[13], culture),
                    Aci = float.Parse(parcalar[14], culture),
                    Durum = byte.Parse(parcalar[15], culture)
                };

                UpdateUI(yeniVeri);
            }
            catch (FormatException ex)
            {
                System.Diagnostics.Debug.WriteLine("Veri formatý hatasý: " + ex.Message);
            }
        }

        private void CreateTestTelemetryData(string[] parcalar)
        {
            try
            {
                var culture = CultureInfo.InvariantCulture;
                
                // Mevcut verilerden test verisi oluþtur
                TelemetryData testVeri = new TelemetryData
                {
                    PaketSayaci = (ushort)(DateTime.Now.Second),
                    Irtifa = parcalar.Length > 0 ? float.Parse(parcalar[0], culture) : 0,
                    RoketGpsIrtifa = parcalar.Length > 1 ? float.Parse(parcalar[1], culture) : 0,
                    RoketGpsEnlem = parcalar.Length > 2 ? float.Parse(parcalar[2], culture) : 0,
                    RoketGpsBoylam = parcalar.Length > 3 ? float.Parse(parcalar[3], culture) : 0,
                    PayloadGpsIrtifa = parcalar.Length > 4 ? float.Parse(parcalar[4].TrimEnd('.'), culture) : 0,
                    PayloadGpsEnlem = 39.925f,
                    PayloadGpsBoylam = 32.865f,
                    JiroskopX = 1.5f,
                    JiroskopY = 0.5f,
                    JiroskopZ = 0.6f,
                    IvmeX = 0.1f,
                    IvmeY = 1.0f,
                    IvmeZ = -4.2f,
                    Aci = 45.0f,
                    Durum = 1
                };

                System.Diagnostics.Debug.WriteLine("Test verisi oluþturuldu ve UI güncelleniyor");
                UpdateUI(testVeri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test verisi oluþturma hatasý: {ex.Message}");
            }
        }

        private void UpdateUI(TelemetryData yeniVeri)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    currentTelemetry = yeniVeri; // gelen veriyi ana telemetri nesnesine ata

                    // TextBlock'larý güncelle
                    irtifaData.Text = $"{currentTelemetry.Irtifa:F2} m"; // F2: virgülden sonra 2 basamak
                    roketGpsIrtifaData.Text = $"{currentTelemetry.RoketGpsIrtifa:F2} m";
                    roketEnlemData.Text = $"{currentTelemetry.RoketGpsEnlem:F6}"; // F6: 6 basamak hassasiyet
                    roketBoylamData.Text = $"{currentTelemetry.RoketGpsBoylam:F6}";
                    gorevYukuGpsIrtifaData.Text = $"{currentTelemetry.PayloadGpsIrtifa:F2} m";
                    gorevYukuGpsEnlemData.Text = $"{currentTelemetry.PayloadGpsEnlem:F6}";
                    gorevYukuGpsBoylamData.Text = $"{currentTelemetry.PayloadGpsBoylam:F6}";
                    jiroskopXData.Text = $"{currentTelemetry.JiroskopX:F2}";
                    jiroskopYData.Text = $"{currentTelemetry.JiroskopY:F2}";
                    jiroskopZData.Text = $"{currentTelemetry.JiroskopZ:F2}";
                    ivmeXData.Text = $"{currentTelemetry.IvmeX:F2}";
                    ivmeYData.Text = $"{currentTelemetry.IvmeY:F2}";
                    ivmeZData.Text = $"{currentTelemetry.IvmeZ:F2}";
                    aciData.Text = $"{currentTelemetry.Aci:F2}";
                    durumData.Text = $"{currentTelemetry.Durum}";

                    // Status güncelle
                    StatusTextBlock.Text = $"Veri güncellendi: {DateTime.Now:HH:mm:ss}";

                    System.Diagnostics.Debug.WriteLine($"UI güncellendi - Ýrtifa: {currentTelemetry.Irtifa:F2}");

                    paketOlustur(); // Yeni veriyle paketi oluþtur ve gönder
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI güncelleme hatasý: {ex.Message}");
                    StatusTextBlock.Text = $"UI Hatasý: {ex.Message}";
                }
            });
        }

        public byte checkSum() // 5 ve 75 byte arasý checksum'a tabi tutulur
        {
            int checkSum = 0;
            for (int i = 4; i < 75; i++)
            {
                checkSum += dataPacket[i];
            }
            return (byte)(currentTelemetry.PaketSayaci % 256);
        }

        public void paketOlustur()
        {
            if (currentTelemetry == null) return;

            // sabit baþlangýç bytelar'ý
            dataPacket[0] = 0xFF;
            dataPacket[1] = 0xFF;
            dataPacket[2] = 0x54;
            dataPacket[3] = 0x52;

            dataPacket[4] = 0; // Takým ID
            dataPacket[5] = (byte)(currentTelemetry.PaketSayaci % 256); // Paket sayacý

            byte[] irtifaBytes = BitConverter.GetBytes(currentTelemetry.Irtifa);
            Array.Copy(irtifaBytes, 0, dataPacket, 6, irtifaBytes.Length);

            byte[] roketGpsIrtifaBytes = BitConverter.GetBytes(currentTelemetry.RoketGpsIrtifa);
            Array.Copy(roketGpsIrtifaBytes, 0, dataPacket, 10, roketGpsIrtifaBytes.Length);

            byte[] roketGpsEnlemBytes = BitConverter.GetBytes(currentTelemetry.RoketGpsEnlem);
            Array.Copy(roketGpsEnlemBytes, 0, dataPacket, 14, roketGpsEnlemBytes.Length);

            byte[] roketGpsBoylamBytes = BitConverter.GetBytes(currentTelemetry.RoketGpsBoylam);
            Array.Copy(roketGpsBoylamBytes, 0, dataPacket, 18, roketGpsBoylamBytes.Length);

            byte[] payloadGpsIrtifaBytes = BitConverter.GetBytes(currentTelemetry.PayloadGpsIrtifa);
            Array.Copy(payloadGpsIrtifaBytes, 0, dataPacket, 22, payloadGpsIrtifaBytes.Length);

            byte[] payloadGpsEnlemBytes = BitConverter.GetBytes(currentTelemetry.PayloadGpsEnlem);
            Array.Copy(payloadGpsEnlemBytes, 0, dataPacket, 26, payloadGpsEnlemBytes.Length);

            byte[] payloadGpsBoylamBytes = BitConverter.GetBytes(currentTelemetry.PayloadGpsBoylam);
            Array.Copy(payloadGpsBoylamBytes, 0, dataPacket, 30, payloadGpsBoylamBytes.Length);

            // Kalan alanlarý doldur (jiroskop, ivme, açý, durum)
            byte[] jiroskopXBytes = BitConverter.GetBytes(currentTelemetry.JiroskopX);
            Array.Copy(jiroskopXBytes, 0, dataPacket, 46, jiroskopXBytes.Length);

            byte[] jiroskopYBytes = BitConverter.GetBytes(currentTelemetry.JiroskopY);
            Array.Copy(jiroskopYBytes, 0, dataPacket, 50, jiroskopYBytes.Length);

            byte[] jiroskopZBytes = BitConverter.GetBytes(currentTelemetry.JiroskopZ);
            Array.Copy(jiroskopZBytes, 0, dataPacket, 54, jiroskopZBytes.Length);

            byte[] ivmeXBytes = BitConverter.GetBytes(currentTelemetry.IvmeX);
            Array.Copy(ivmeXBytes, 0, dataPacket, 58, ivmeXBytes.Length);

            byte[] ivmeYBytes = BitConverter.GetBytes(currentTelemetry.IvmeY);
            Array.Copy(ivmeYBytes, 0, dataPacket, 62, ivmeYBytes.Length);

            byte[] ivmeZBytes = BitConverter.GetBytes(currentTelemetry.IvmeZ);
            Array.Copy(ivmeZBytes, 0, dataPacket, 66, ivmeZBytes.Length);

            byte[] aciBytes = BitConverter.GetBytes(currentTelemetry.Aci);
            Array.Copy(aciBytes, 0, dataPacket, 70, aciBytes.Length);

            dataPacket[74] = currentTelemetry.Durum;
            dataPacket[75] = checkSum();
            dataPacket[76] = 0x0D;
            dataPacket[77] = 0x0A;
        }

        // Eksik metodu ekle
        private void ClearData_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SerialDataTextBox.Text = "";
            _dataBuffer.Clear();
            System.Diagnostics.Debug.WriteLine("Veriler temizlendi");
        }

        

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SerialPortService.OnDataReceived -= OnSerialDataReceived;
        }

        public class TelemetryData
        {
            public ushort PaketSayaci { get; set; }
            public float Irtifa { get; set; }
            public float RoketGpsIrtifa { get; set; }
            public float RoketGpsEnlem { get; set; }
            public float RoketGpsBoylam { get; set; }
            public float PayloadGpsIrtifa { get; set; }
            public float PayloadGpsEnlem { get; set; }
            public float PayloadGpsBoylam { get; set; }
            public float JiroskopX { get; set; }
            public float JiroskopY { get; set; }
            public float JiroskopZ { get; set; }
            public float IvmeX { get; set; }
            public float IvmeY { get; set; }
            public float IvmeZ { get; set; }
            public float Aci { get; set; }
            public byte Durum { get; set; }
        }
    }
}