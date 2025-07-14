using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System.IO.Ports;
using System;
using copilot_deneme.ViewModels;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml.Navigation;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using Microsoft.Web.WebView2.Core;

namespace copilot_deneme
{
    /// <summary>
    /// SUT (Sistem Uydu Test) sayfasý - Ayrý chart'lar ve GPS haritasý
    /// </summary>
    public sealed partial class sutPage : Page
    {
        private readonly ChartViewModel _viewModel;
        private readonly DispatcherQueue _dispatcherQueue;
        private TelemetryData currentTelemetry = new TelemetryData();
        private byte[] dataPacket = new byte[78];
        private StringBuilder _dataBuffer = new StringBuilder();

        // Her veri için ayrý koleksiyonlar
        private const int MaxDataPoints = 50; // Chart'larda gösterilecek maksimum nokta
        
        private readonly ObservableCollection<ObservableValue> _irtifaValues = new();
        private readonly ObservableCollection<ObservableValue> _roketGpsIrtifaValues = new();
        private readonly ObservableCollection<ObservableValue> _payloadGpsIrtifaValues = new();
        private readonly ObservableCollection<ObservableValue> _roketGpsBoylamValues = new();
        private readonly ObservableCollection<ObservableValue> _payloadGpsEnlemValues = new();
        private readonly ObservableCollection<ObservableValue> _payloadGpsBoylamValues = new();
        private readonly ObservableCollection<ObservableValue> _jiroskopXValues = new();
        private readonly ObservableCollection<ObservableValue> _jiroskopYValues = new();
        private readonly ObservableCollection<ObservableValue> _jiroskopZValues = new();
        private readonly ObservableCollection<ObservableValue> _ivmeXValues = new();
        private readonly ObservableCollection<ObservableValue> _ivmeYValues = new();
        private readonly ObservableCollection<ObservableValue> _ivmeZValues = new();
        private readonly ObservableCollection<ObservableValue> _aciValues = new();

        // GPS Harita deðiþkenleri
        private bool _isMapInitialized = false;
        private double _currentRocketLat = 39.925533;  // Ankara baþlangýç konumu
        private double _currentRocketLon = 32.866287;
        private double _currentPayloadLat = 39.925533;
        private double _currentPayloadLon = 32.866287;

        public sutPage()
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
            
            // Chart'larý baþlat
            InitializeCharts();
            
            // GPS haritasýný baþlat
            InitializeGpsMap();
        }

        private async void InitializeGpsMap()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                
                // HTML harita sayfasý oluþtur
                string mapHtml = CreateGpsMapHtml();
                MapWebView.NavigateToString(mapHtml);
                
                _isMapInitialized = true;
                System.Diagnostics.Debug.WriteLine("GPS harita baþarýyla baþlatýldý");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPS harita baþlatma hatasý: {ex.Message}");
            }
        }

        private string CreateGpsMapHtml()
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>SUT GPS Takip Sistemi</title>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body {{ margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
        #map {{ height: 100vh; width: 100%; }}
        .custom-div-icon {{ 
            background: none; 
            border: none; 
        }}
        .rocket-marker {{
            background: #ff4444;
            width: 14px;
            height: 14px;
            border-radius: 50%;
            border: 3px solid white;
            box-shadow: 0 0 6px rgba(255,68,68,0.8);
            animation: pulse-red 2s infinite;
        }}
        .payload-marker {{
            background: #44ff44;
            width: 14px;
            height: 14px;
            border-radius: 50%;
            border: 3px solid white;
            box-shadow: 0 0 6px rgba(68,255,68,0.8);
            animation: pulse-green 2s infinite;
        }}
        @keyframes pulse-red {{
            0% {{ box-shadow: 0 0 6px rgba(255,68,68,0.8); }}
            50% {{ box-shadow: 0 0 12px rgba(255,68,68,1), 0 0 18px rgba(255,68,68,0.5); }}
            100% {{ box-shadow: 0 0 6px rgba(255,68,68,0.8); }}
        }}
        @keyframes pulse-green {{
            0% {{ box-shadow: 0 0 6px rgba(68,255,68,0.8); }}
            50% {{ box-shadow: 0 0 12px rgba(68,255,68,1), 0 0 18px rgba(68,255,68,0.5); }}
            100% {{ box-shadow: 0 0 6px rgba(68,255,68,0.8); }}
        }}
        .info-panel {{
            position: absolute;
            top: 10px;
            right: 10px;
            background: rgba(0,0,0,0.8);
            color: white;
            padding: 8px 12px;
            border-radius: 6px;
            font-size: 11px;
            z-index: 1000;
            min-width: 140px;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <div class='info-panel'>
        <div style='font-weight: bold; margin-bottom: 4px;'>?? SUT GPS Takip</div>
        <div>?? Roket: <span id='rocket-coords'>39.9255, 32.8663</span></div>
        <div>?? Payload: <span id='payload-coords'>39.9255, 32.8663</span></div>
        <div style='margin-top: 4px; font-size: 10px; opacity: 0.8;'>Son güncelleme: <span id='last-update'>-</span></div>
    </div>
    
    <script>
        // Harita oluþtur - Ankara merkezli
        var map = L.map('map').setView([39.925533, 32.866287], 12);
        
        // OpenStreetMap tile layer ekle
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap contributors | SUT Telemetri Sistemi',
            maxZoom: 18
        }}).addTo(map);
        
        // Roket marker'ý (kýrmýzý)
        var rocketMarker = L.marker([39.925533, 32.866287], {{
            icon: L.divIcon({{
                html: '<div class=""rocket-marker""></div>',
                iconSize: [20, 20],
                className: 'custom-div-icon'
            }}),
            title: 'Roket Konumu'
        }}).addTo(map);
        
        // Payload marker'ý (yeþil)
        var payloadMarker = L.marker([39.925533, 32.866287], {{
            icon: L.divIcon({{
                html: '<div class=""payload-marker""></div>',
                iconSize: [20, 20],
                className: 'custom-div-icon'
            }}),
            title: 'Payload Konumu'
        }}).addTo(map);
        
        // Roket uçuþ yolu (kýrmýzý çizgi)
        var rocketPath = L.polyline([], {{ 
            color: '#ff4444', 
            weight: 3, 
            opacity: 0.8,
            dashArray: '5, 5'
        }}).addTo(map);
        
        // Payload uçuþ yolu (yeþil çizgi)
        var payloadPath = L.polyline([], {{ 
            color: '#44ff44', 
            weight: 3, 
            opacity: 0.8,
            dashArray: '5, 5'
        }}).addTo(map);
        
        // Tooltip'ler ekle
        rocketMarker.bindTooltip('?? Roket', {{ permanent: false, direction: 'top' }});
        payloadMarker.bindTooltip('?? Payload', {{ permanent: false, direction: 'top' }});
        
        // C# tarafýndan çaðrýlacak JavaScript fonksiyonlarý
        window.updateRocketPosition = function(lat, lon) {{
            if (lat !== 0 && lon !== 0) {{
                var newPos = [lat, lon];
                rocketMarker.setLatLng(newPos);
                rocketPath.addLatLng(newPos);
                
                // Koordinat bilgisini güncelle
                document.getElementById('rocket-coords').textContent = lat.toFixed(6) + ', ' + lon.toFixed(6);
                updateLastUpdateTime();
                
                // Harita görünümünü güncelle
                fitMapToBounds();
            }}
        }};
        
        window.updatePayloadPosition = function(lat, lon) {{
            if (lat !== 0 && lon !== 0) {{
                var newPos = [lat, lon];
                payloadMarker.setLatLng(newPos);
                payloadPath.addLatLng(newPos);
                
                // Koordinat bilgisini güncelle
                document.getElementById('payload-coords').textContent = lat.toFixed(6) + ', ' + lon.toFixed(6);
                updateLastUpdateTime();
                
                // Harita görünümünü güncelle
                fitMapToBounds();
            }}
        }};
        
        window.updateBothPositions = function(rocketLat, rocketLon, payloadLat, payloadLon) {{
            var updated = false;
            
            if (rocketLat !== 0 && rocketLon !== 0) {{
                var rocketPos = [rocketLat, rocketLon];
                rocketMarker.setLatLng(rocketPos);
                rocketPath.addLatLng(rocketPos);
                document.getElementById('rocket-coords').textContent = rocketLat.toFixed(6) + ', ' + rocketLon.toFixed(6);
                updated = true;
            }}
            
            if (payloadLat !== 0 && payloadLon !== 0) {{
                var payloadPos = [payloadLat, payloadLon];
                payloadMarker.setLatLng(payloadPos);
                payloadPath.addLatLng(payloadPos);
                document.getElementById('payload-coords').textContent = payloadLat.toFixed(6) + ', ' + payloadLon.toFixed(6);
                updated = true;
            }}
            
            if (updated) {{
                updateLastUpdateTime();
                fitMapToBounds();
            }}
        }};
        
        function fitMapToBounds() {{
            var group = new L.featureGroup([rocketMarker, payloadMarker]);
            var bounds = group.getBounds();
            if (bounds.isValid()) {{
                map.fitBounds(bounds.pad(0.1));
            }}
        }}
        
        function updateLastUpdateTime() {{
            var now = new Date();
            var timeStr = now.getHours().toString().padStart(2, '0') + ':' + 
                         now.getMinutes().toString().padStart(2, '0') + ':' + 
                         now.getSeconds().toString().padStart(2, '0');
            document.getElementById('last-update').textContent = timeStr;
        }}
        
        // Ýlk güncelleme
        updateLastUpdateTime();
    </script>
</body>
</html>";
        }

        private async void UpdateGpsPositions(double rocketLat, double rocketLon, double payloadLat, double payloadLon)
        {
            if (!_isMapInitialized) return;
            
            try
            {
                // Sadece geçerli koordinatlarý güncelle
                if (rocketLat != 0 && rocketLon != 0)
                {
                    _currentRocketLat = rocketLat;
                    _currentRocketLon = rocketLon;
                }
                
                if (payloadLat != 0 && payloadLon != 0)
                {
                    _currentPayloadLat = payloadLat;
                    _currentPayloadLon = payloadLon;
                }
                
                // JavaScript fonksiyonunu çaðýr
                string script = $"updateBothPositions({_currentRocketLat.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentRocketLon.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentPayloadLat.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentPayloadLon.ToString(CultureInfo.InvariantCulture)})";
                
                await MapWebView.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"GPS pozisyonlarý güncellendi - Roket: {_currentRocketLat:F6}, {_currentRocketLon:F6} | Payload: {_currentPayloadLat:F6}, {_currentPayloadLon:F6}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPS pozisyon güncelleme hatasý: {ex.Message}");
            }
        }

        private void InitializeCharts()
        {
            try
            {
                // Ýrtifa Chart
                IrtifaChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Ýrtifa",
                        Values = _irtifaValues,
                        Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Roket GPS Ýrtifa Chart
                RoketGpsIrtifaChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Roket GPS Ýrtifa",
                        Values = _roketGpsIrtifaValues,
                        Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Payload GPS Ýrtifa Chart
                PayloadGpsIrtifaChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Payload GPS Ýrtifa",
                        Values = _payloadGpsIrtifaValues,
                        Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Roket GPS Boylam Chart
                RoketGpsBoylamChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Roket GPS Boylam",
                        Values = _roketGpsBoylamValues,
                        Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Payload GPS Enlem Chart
                PayloadGpsEnlemChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Payload GPS Enlem",
                        Values = _payloadGpsEnlemValues,
                        Stroke = new SolidColorPaint(SKColors.DarkOrange) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Payload GPS Boylam Chart
                PayloadGpsBoylamChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Payload GPS Boylam",
                        Values = _payloadGpsBoylamValues,
                        Stroke = new SolidColorPaint(SKColors.DarkViolet) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Jiroskop X Chart
                JiroskopXChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Jiroskop X",
                        Values = _jiroskopXValues,
                        Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Jiroskop Y Chart
                JiroskopYChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Jiroskop Y",
                        Values = _jiroskopYValues,
                        Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Jiroskop Z Chart
                JiroskopZChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Jiroskop Z",
                        Values = _jiroskopZValues,
                        Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Ývme X Chart
                IvmeXChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Ývme X",
                        Values = _ivmeXValues,
                        Stroke = new SolidColorPaint(SKColors.Crimson) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Ývme Y Chart
                IvmeYChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Ývme Y",
                        Values = _ivmeYValues,
                        Stroke = new SolidColorPaint(SKColors.ForestGreen) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Ývme Z Chart
                IvmeZChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Ývme Z",
                        Values = _ivmeZValues,
                        Stroke = new SolidColorPaint(SKColors.RoyalBlue) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                // Açý Chart
                AciChart.Series = new ISeries[]
                {
                    new LineSeries<ObservableValue>
                    {
                        Name = "Açý",
                        Values = _aciValues,
                        Stroke = new SolidColorPaint(SKColors.DarkMagenta) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"sutPage chart baþlatma hatasý: {ex.Message}");
            }
        }

        private void OnSerialDataReceived(string data)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                _dataBuffer.Append(data);
                ProcessBuffer();
            });
        }

        private void ProcessBuffer()
        {
            string bufferContent = _dataBuffer.ToString();
            
            string[] lines = bufferContent.Split('\n');
            
            _dataBuffer.Clear();
            if (lines.Length > 0 && !bufferContent.EndsWith('\n'))
            {
                _dataBuffer.Append(lines[lines.Length - 1]);
                Array.Resize(ref lines, lines.Length - 1);
            }

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
            gelenSatir = gelenSatir.Replace("\r", "").Replace("\n", "").Trim();
            
            if (string.IsNullOrEmpty(gelenSatir))
                return;

            string[] parcalar = gelenSatir.Split(',');

            if (parcalar.Length < 16)
            {
                if (parcalar.Length >= 5)
                {
                    CreateTestTelemetryData(parcalar);
                }
                return;
            }

            try
            {
                var culture = CultureInfo.InvariantCulture;

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

                UpdateCharts(yeniVeri);
            }
            catch (FormatException)
            {
                // Sessizce atla
            }
        }

        private void CreateTestTelemetryData(string[] parcalar)
        {
            try
            {
                var culture = CultureInfo.InvariantCulture;
                
                TelemetryData testVeri = new TelemetryData
                {
                    PaketSayaci = (ushort)(DateTime.Now.Second),
                    Irtifa = parcalar.Length > 0 ? float.Parse(parcalar[0], culture) : 0,
                    RoketGpsIrtifa = parcalar.Length > 1 ? float.Parse(parcalar[1], culture) : 0,
                    RoketGpsEnlem = parcalar.Length > 2 ? float.Parse(parcalar[2], culture) : 39.925533f,
                    RoketGpsBoylam = parcalar.Length > 3 ? float.Parse(parcalar[3], culture) : 32.866287f,
                    PayloadGpsIrtifa = parcalar.Length > 4 ? float.Parse(parcalar[4].TrimEnd('.'), culture) : 0,
                    PayloadGpsEnlem = 39.925533f,
                    PayloadGpsBoylam = 32.866287f,
                    JiroskopX = 1.5f,
                    JiroskopY = 0.5f,
                    JiroskopZ = 0.6f,
                    IvmeX = 0.1f,
                    IvmeY = 1.0f,
                    IvmeZ = -4.2f,
                    Aci = 45.0f,
                    Durum = 1
                };

                UpdateCharts(testVeri);
            }
            catch (Exception)
            {
                // Sessizce atla
            }
        }

        private void UpdateCharts(TelemetryData yeniVeri)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    currentTelemetry = yeniVeri;
                    UpdateIndividualCharts(currentTelemetry);
                    SendDataToMainCharts(currentTelemetry);
                    
                    // GPS haritasýný güncelle - gerçek koordinatlar
                    UpdateGpsPositions(currentTelemetry.RoketGpsEnlem, currentTelemetry.RoketGpsBoylam,
                                     currentTelemetry.PayloadGpsEnlem, currentTelemetry.PayloadGpsBoylam);
                    
                    paketOlustur();
                }
                catch (Exception)
                {
                    // Sessizce atla
                }
            });
        }

        private void UpdateIndividualCharts(TelemetryData telemetryData)
        {
            try
            {
                AddDataToChart(_irtifaValues, telemetryData.Irtifa);
                AddDataToChart(_roketGpsIrtifaValues, telemetryData.RoketGpsIrtifa);
                AddDataToChart(_payloadGpsIrtifaValues, telemetryData.PayloadGpsIrtifa);
                AddDataToChart(_roketGpsBoylamValues, telemetryData.RoketGpsBoylam);
                AddDataToChart(_payloadGpsEnlemValues, telemetryData.PayloadGpsEnlem);
                AddDataToChart(_payloadGpsBoylamValues, telemetryData.PayloadGpsBoylam);
                AddDataToChart(_jiroskopXValues, telemetryData.JiroskopX);
                AddDataToChart(_jiroskopYValues, telemetryData.JiroskopY);
                AddDataToChart(_jiroskopZValues, telemetryData.JiroskopZ);
                AddDataToChart(_ivmeXValues, telemetryData.IvmeX);
                AddDataToChart(_ivmeYValues, telemetryData.IvmeY);
                AddDataToChart(_ivmeZValues, telemetryData.IvmeZ);
                AddDataToChart(_aciValues, telemetryData.Aci);
            }
            catch (Exception)
            {
                // Sessizce atla
            }
        }

        private void AddDataToChart(ObservableCollection<ObservableValue> collection, float value)
        {
            collection.Add(new ObservableValue(value));
            if (collection.Count > MaxDataPoints)
            {
                collection.RemoveAt(0);
            }
        }

        private void SendDataToMainCharts(TelemetryData telemetryData)
        {
            try
            {
                SerialPortService.UpdateChartsFromExternalData(
                    telemetryData.Irtifa,             
                    telemetryData.PayloadGpsIrtifa,   
                    telemetryData.IvmeZ,              
                    telemetryData.IvmeZ,              
                    0,                                
                    0,                                
                    25,                               
                    25,                               
                    1013,                             
                    1013,                             
                    50,                               
                    "sutPage",                        
                    telemetryData.JiroskopX,          
                    telemetryData.JiroskopY,          
                    telemetryData.JiroskopZ,          
                    telemetryData.IvmeX,              
                    telemetryData.IvmeY,              
                    telemetryData.Aci                 
                );
            }
            catch (Exception)
            {
                // Sessizce atla
            }
        }

        public byte checkSum()
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

            dataPacket[0] = 0xFF;
            dataPacket[1] = 0xFF;
            dataPacket[2] = 0x54;
            dataPacket[3] = 0x52;

            dataPacket[4] = 0;
            dataPacket[5] = (byte)(currentTelemetry.PaketSayaci % 256);

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
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
