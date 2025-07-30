using copilot_deneme.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;
using System.Threading.Tasks;


namespace copilot_deneme
{
    /// <summary>
    /// SÝT Telemetri verilerini ve GPS haritasýný görüntüleyen sayfa
    /// </summary>
    public sealed partial class sitPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private ChartViewModel _viewModel = new ChartViewModel();

        // Ýstatistik deðiþkenleri
        private float _maxAltitude = 0;
        private int _Counter = 0;
        private int _CRC = 0;
        private int _TeamID = 0;

        // GPS Harita deðiþkenleri
        private bool _isMapInitialized = false;
        private double _currentRocketLat = 39.925533;  // Ankara baþlangýç konumu
        private double _currentRocketLon = 32.866287;
        private double _currentPayloadLat = 39.925533;
        private double _currentPayloadLon = 32.866287;

        public sitPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            
            // SerialPortService'den telemetri veri güncellemelerini dinle
            SerialPortService.OnTelemetryDataUpdated += OnTelemetryDataUpdated;
            SerialPortService.OnDataReceived += OnSerialDataReceived;
            SerialPortService.OnRotationDataReceived += OnRotationDataReceived;

            InitializeDisplay();
            InitializeThreeDWebView();
       
            InitializeGpsMap();
        }
        private async void InitializeThreeDWebView()
        {
            await ThreeDWebView.EnsureCoreWebView2Async();
            string assetPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "assets");
            ThreeDWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "assets.local", assetPath, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
            ThreeDWebView.NavigateToString(HtmlTemplate);
        }

        private void OnRotationDataReceived(float yaw, float pitch, float roll)
        {
           
            // Gelen verinin UI thread'inde iþlendiðinden emin ol
            _dispatcherQueue.TryEnqueue(async () =>
            {
                // 3D modeli güncellemek için mevcut metodunuzu çaðýrýn
                await UpdateRotationAsync(yaw, pitch, roll);
            });
        }

        // Telemetri verisi geldikçe bu metodu çaðýrýn:
        public async Task UpdateRotationAsync(float yaw, float pitch, float roll)
        {
            if (ThreeDWebView.CoreWebView2 == null)
                return;

            // JS fonksiyonunu çaðýr
            string script = $"updateModelRotation({yaw.ToString(CultureInfo.InvariantCulture)}, " +
                                           $"{pitch.ToString(CultureInfo.InvariantCulture)}, " +
                                           $"{roll.ToString(CultureInfo.InvariantCulture)});";
            await ThreeDWebView.ExecuteScriptAsync(script);
        }
        private async void InitializeGpsMap()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                
              
                string mapHtml = CreateSitPageMapHtml();
                MapWebView.NavigateToString(mapHtml);
                
                _isMapInitialized = true;
                System.Diagnostics.Debug.WriteLine("sitPage GPS harita baþarýyla baþlatýldý");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"sitPage GPS harita baþlatma hatasý: {ex.Message}");
            }
        }

        private string CreateSitPageMapHtml()
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Roket GPS Takip Sistemi - Ana Harita</title>
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
            width: 18px;
            height: 18px;
            border-radius: 50%;
            border: 4px solid white;
            box-shadow: 0 0 8px rgba(255,68,68,0.9);
            animation: pulse-red 2s infinite;
        }}
        .payload-marker {{
            background: #44ff44;
            width: 18px;
            height: 18px;
            border-radius: 50%;
            border: 4px solid white;
            box-shadow: 0 0 8px rgba(68,255,68,0.9);
            animation: pulse-green 2s infinite;
        }}
        @keyframes pulse-red {{
            0% {{ box-shadow: 0 0 8px rgba(255,68,68,0.9); }}
            50% {{ box-shadow: 0 0 16px rgba(255,68,68,1), 0 0 24px rgba(255,68,68,0.6); }}
            100% {{ box-shadow: 0 0 8px rgba(255,68,68,0.9); }}
        }}
        @keyframes pulse-green {{
            0% {{ box-shadow: 0 0 8px rgba(68,255,68,0.9); }}
            50% {{ box-shadow: 0 0 16px rgba(68,255,68,1), 0 0 24px rgba(68,255,68,0.6); }}
            100% {{ box-shadow: 0 0 8px rgba(68,255,68,0.9); }}
        }}
        .info-panel {{
            position: absolute;
            top: 15px;
            right: 15px;
            background: rgba(0,0,0,0.85);
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            font-size: 13px;
            z-index: 1000;
            min-width: 200px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
        }}
        .info-title {{
            font-weight: bold;
            font-size: 14px;
            margin-bottom: 8px;
            color: #64B5F6;
        }}
        .coord-row {{
            margin: 4px 0;
            display: flex;
            justify-content: space-between;
        }}
        .altitude-panel {{
            position: absolute;
            top: 15px;
            left: 15px;
            background: rgba(0,0,0,0.85);
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            font-size: 12px;
            z-index: 1000;
            min-width: 160px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.3);
        }}
        .alt-title {{
            font-weight: bold;
            font-size: 13px;
            margin-bottom: 6px;
            color: #81C784;
        }}
        .legend-panel {{
            position: absolute;
            bottom: 15px;
            right: 15px;
            background: rgba(0,0,0,0.85);
            color: white;
            padding: 10px 14px;
            border-radius: 8px;
            font-size: 11px;
            z-index: 1000;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    
    <div class='info-panel'>
        <div class='info-title'>??? GPS Koordinatlarý</div>
        <div class='coord-row'>
            <span>?? Roket:</span>
            <span id='rocket-coords'>39.9255, 32.8663</span>
        </div>
        <div class='coord-row'>
            <span>?? Payload:</span>
            <span id='payload-coords'>39.9255, 32.8663</span>
        </div>
        <div style='margin-top: 8px; font-size: 11px; opacity: 0.8; text-align: center;'>
            Son güncelleme: <span id='last-update'>-</span>
        </div>
    </div>

    <div class='altitude-panel'>
        <div class='alt-title'>?? Ýrtifa Bilgileri</div>
        <div class='coord-row'>
            <span>?? Roket:</span>
            <span id='rocket-alt'>0.00 m</span>
        </div>
        <div class='coord-row'>
            <span>?? Payload:</span>
            <span id='payload-alt'>0.00 m</span>
        </div>
        <div class='coord-row'>
            <span>?? Mesafe:</span>
            <span id='distance'>0.00 m</span>
        </div>
    </div>

    <div class='legend-panel'>
        <div style='font-weight: bold; margin-bottom: 4px;'>?? Açýklama</div>
        <div>?? Roket Konumu</div>
        <div>?? Payload Konumu</div>
        <div>?? Uçuþ Rotalarý</div>
    </div>
    
    <script>
        // Harita oluþtur - Ankara merkezli
        var map = L.map('map').setView([39.925533, 32.866287], 13);
        
        // OpenStreetMap tile layer ekle
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '© OpenStreetMap | SÝT Telemetri Sistemi',
            maxZoom: 18
        }}).addTo(map);
        
        // Roket marker'ý (kýrmýzý)
        var rocketMarker = L.marker([39.925533, 32.866287], {{
            icon: L.divIcon({{
                html: '<div class=""rocket-marker""></div>',
                iconSize: [26, 26],
                className: 'custom-div-icon'
            }}),
            title: 'Roket Konumu'
        }}).addTo(map);
        
        // Payload marker'ý (yeþil)
        var payloadMarker = L.marker([39.925533, 32.866287], {{
            icon: L.divIcon({{
                html: '<div class=""payload-marker""></div>',
                iconSize: [26, 26],
                className: 'custom-div-icon'
            }}),
            title: 'Payload Konumu'
        }}).addTo(map);
        
        // Roket uçuþ yolu (kýrmýzý çizgi)
        var rocketPath = L.polyline([], {{ 
            color: '#ff4444', 
            weight: 4, 
            opacity: 0.8,
            dashArray: '8, 8'
        }}).addTo(map);
        
        // Payload uçuþ yolu (yeþil çizgi)
        var payloadPath = L.polyline([], {{ 
            color: '#44ff44', 
            weight: 4, 
            opacity: 0.8,
            dashArray: '8, 8'
        }}).addTo(map);
        
        // Tooltip'ler ekle
        rocketMarker.bindTooltip('?? Roket Aracý', {{ permanent: false, direction: 'top' }});
        payloadMarker.bindTooltip('?? Payload Aracý', {{ permanent: false, direction: 'top' }});
        
        // Mesafe hesaplama
        function calculateDistance(lat1, lon1, lat2, lon2) {{
            var R = 6371000; // Dünya yarýçapý metre cinsinden
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.sin(dLat/2) * Math.sin(dLat/2) +
                    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                    Math.sin(dLon/2) * Math.sin(dLon/2);
            var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
            return R * c;
        }}
        
        // C# tarafýndan çaðrýlacak JavaScript fonksiyonlarý
        window.updateRocketPosition = function(lat, lon, alt) {{
            if (lat !== 0 && lon !== 0) {{
                var newPos = [lat, lon];
                rocketMarker.setLatLng(newPos);
                rocketPath.addLatLng(newPos);
                
                // Koordinat bilgisini güncelle
                document.getElementById('rocket-coords').textContent = lat.toFixed(6) + ', ' + lon.toFixed(6);
                document.getElementById('rocket-alt').textContent = alt.toFixed(2) + ' m';
                updateLastUpdateTime();
                updateDistance();
                fitMapToBounds();
            }}
        }};
        
        window.updatePayloadPosition = function(lat, lon, alt) {{
            if (lat !== 0 && lon !== 0) {{
                var newPos = [lat, lon];
                payloadMarker.setLatLng(newPos);
                payloadPath.addLatLng(newPos);
                
                // Koordinat bilgisini güncelle
                document.getElementById('payload-coords').textContent = lat.toFixed(6) + ', ' + lon.toFixed(6);
                document.getElementById('payload-alt').textContent = alt.toFixed(2) + ' m';
                updateLastUpdateTime();
                updateDistance();
                fitMapToBounds();
            }}
        }};
        
        window.updateBothPositions = function(rocketLat, rocketLon, rocketAlt, payloadLat, payloadLon, payloadAlt) {{
            var updated = false;
            
            if (rocketLat !== 0 && rocketLon !== 0) {{
                var rocketPos = [rocketLat, rocketLon];
                rocketMarker.setLatLng(rocketPos);
                rocketPath.addLatLng(rocketPos);
                document.getElementById('rocket-coords').textContent = rocketLat.toFixed(6) + ', ' + rocketLon.toFixed(6);
                document.getElementById('rocket-alt').textContent = rocketAlt.toFixed(2) + ' m';
                updated = true;
            }}
            
            if (payloadLat !== 0 && payloadLon !== 0) {{
                var payloadPos = [payloadLat, payloadLon];
                payloadMarker.setLatLng(payloadPos);
                payloadPath.addLatLng(payloadPos);
                document.getElementById('payload-coords').textContent = payloadLat.toFixed(6) + ', ' + payloadLon.toFixed(6);
                document.getElementById('payload-alt').textContent = payloadAlt.toFixed(2) + ' m';
                updated = true;
            }}
            
            if (updated) {{
                updateLastUpdateTime();
                updateDistance();
                fitMapToBounds();
            }}
        }};
        
        function updateDistance() {{
            var rocketPos = rocketMarker.getLatLng();
            var payloadPos = payloadMarker.getLatLng();
            var distance = calculateDistance(rocketPos.lat, rocketPos.lng, payloadPos.lat, payloadPos.lng);
            document.getElementById('distance').textContent = distance.toFixed(2) + ' m';
        }}
        
        function fitMapToBounds() {{
            var group = new L.featureGroup([rocketMarker, payloadMarker]);
            var bounds = group.getBounds();
            if (bounds.isValid()) {{
                map.fitBounds(bounds.pad(0.15));
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
        updateDistance();
    </script>
</body>
</html>";
        }

        private async void UpdateSitPageGpsPositions(double rocketLat, double rocketLon, double rocketAlt, double payloadLat, double payloadLon, double payloadAlt)
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
                
                // JavaScript fonksiyonunu çaðýr - irtifa bilgisi ile birlikte
                string script = $"updateBothPositions({_currentRocketLat.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentRocketLon.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{rocketAlt.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentPayloadLat.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{_currentPayloadLon.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{payloadAlt.ToString(CultureInfo.InvariantCulture)})";
                
                await MapWebView.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"sitPage GPS pozisyonlarý güncellendi - Roket: {_currentRocketLat:F6}, {_currentRocketLon:F6} ({rocketAlt:F2}m) | Payload: {_currentPayloadLat:F6}, {_currentPayloadLon:F6} ({payloadAlt:F2}m)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"sitPage GPS pozisyon güncelleme hatasý: {ex.Message}");
            }
        }
        private const string HtmlTemplate = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <title>3D Scene with STL</title>
  <style>
    body, html { margin: 0; padding: 0; overflow: hidden; background-color: #1e1e1e; }
    canvas { display: block; }
  </style>
</head>
<body>
  <script src=""https://cdn.jsdelivr.net/npm/three@0.142.0/build/three.min.js""></script>
  <script src=""https://cdn.jsdelivr.net/npm/three@0.142.0/examples/js/loaders/STLLoader.js""></script>
  
  <script>
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(50, window.innerWidth / window.innerHeight, 0.1, 1000);
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    document.body.appendChild(renderer.domElement);

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
    const dirLight = new THREE.DirectionalLight(0xffffff, 1.0);
    dirLight.position.set(5, 10, 7.5);
    scene.add(ambientLight, dirLight);

    camera.position.set(5, 5, 5);
camera.lookAt(0, 0, 0);
camera.up.set(0, 1, 0)


    let model;
    const loader = new THREE.STLLoader();

   loader.load(
    'https://assets.local/rocket_model.stl',
    function (geometry) {
        geometry.computeBoundingBox();
        const center = new THREE.Vector3();
        geometry.boundingBox.getCenter(center).negate();
        geometry.translate(center.x, center.y, center.z);

        const material = new THREE.MeshStandardMaterial({
            color: 0x0077ff,
            metalness: 0.3,
            roughness: 0.5,
            side: THREE.DoubleSide
        });
        const mesh = new THREE.Mesh(geometry, material);

        // Otomatik scale (çok büyük/küçük model varsa normalize et)
        geometry.computeBoundingBox();
        const size = geometry.boundingBox.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const scale = 5.0 / maxDim;
        mesh.scale.set(scale, scale, scale);

       
        scene.add(mesh);
        model = mesh;

        console.log('STL model yüklendi ve ortalandý.');
    },
    function (xhr) { console.log((xhr.loaded / xhr.total * 100) + '% loaded'); },
    function (error) { console.error('STL yüklenirken hata:', error); }
);


    function animate() {
        requestAnimationFrame(animate);
        renderer.render(scene, camera);
    }
    animate();

    window.updateModelRotation = (yaw, pitch, roll) => {
        if (model) {
            model.rotation.y = yaw * Math.PI / 180;
            model.rotation.x = pitch * Math.PI / 180;
            model.rotation.z = roll * Math.PI / 180;
        }
    };

    window.addEventListener('resize', () => {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    });
  </script>
</body>
</html>";

        private void InitializeDisplay()
        {
            // Ýlk deðerleri ayarla
            LastUpdateText.Text = "Baðlantý bekleniyor...";
            DataCountText.Text = "0";
            MaxAltitudeText.Text = "0.00 m";
            CRCText.Text = "0";
            TeamIDText.Text = "0";
            
        }

        private void OnSerialDataReceived(string data)
        {
            // Bu sadece baðlantý durumunu göstermek için
            _dispatcherQueue.TryEnqueue(() =>
            {
                LastUpdateText.Text = $"Veri alýyor: {DateTime.Now:HH:mm:ss}";
            });
        }

        private void OnTelemetryDataUpdated(SerialPortService.TelemetryUpdateData telemetryData)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Roket verileri
                    RocketAltitudeText.Text = $"{telemetryData.RocketAltitude:F2} m";
                    RocketGpsAltitudeText.Text = $"{telemetryData.RocketGpsAltitude:F2} m";
                    RocketLatitudeText.Text = $"{telemetryData.RocketLatitude:F6}";
                    RocketLongitudeText.Text = $"{telemetryData.RocketLongitude:F6}";
                    RocketSpeedText.Text = $"{telemetryData.RocketSpeed:F2} m/s";
                    RocketTemperatureText.Text = $"{telemetryData.RocketTemperature:F1} °C";
                    RocketPressureText.Text = $"{telemetryData.RocketPressure:F1} hPa";

                    // Payload verileri
                    PayloadAltitudeText.Text = $"{telemetryData.PayloadAltitude:F2} m";
                    PayloadLatitudeText.Text = $"{telemetryData.PayloadLatitude:F6}";
                    PayloadLongitudeText.Text = $"{telemetryData.PayloadLongitude:F6}";
                    PayloadSpeedText.Text = $"{telemetryData.PayloadSpeed:F2} m/s";
                    PayloadTemperatureText.Text = $"{telemetryData.PayloadTemperature:F1} °C";
                    PayloadPressureText.Text = $"{telemetryData.PayloadPressure:F1} hPa";
                    PayloadHumidityText.Text = $"{telemetryData.PayloadHumidity:F1} %";

                    // Jiroskop verileri
                    GyroXText.Text = $"{telemetryData.GyroX:F2} °/s";
                    GyroYText.Text = $"{telemetryData.GyroY:F2} °/s";
                    GyroZText.Text = $"{telemetryData.GyroZ:F2} °/s";

                    // Ývme verileri
                    AccelXText.Text = $"{telemetryData.AccelX:F2} m/s²";
                    AccelYText.Text = $"{telemetryData.AccelY:F2} m/s²";
                    AccelZText.Text = $"{telemetryData.AccelZ:F2} m/s²";
                    AngleText.Text = $"{telemetryData.Angle:F2}°";

                    // GPS haritasýný güncelle - gerçek koordinatlar ve irtifa bilgisi ile
                    UpdateSitPageGpsPositions(telemetryData.RocketLatitude, telemetryData.RocketLongitude, telemetryData.RocketAltitude,
                                            telemetryData.PayloadLatitude, telemetryData.PayloadLongitude, telemetryData.PayloadAltitude);

                    // Chart'lara veri gönder
                    SendDataToCharts(telemetryData);

                    // Ýstatistikleri güncelle
                    UpdateStatistics(telemetryData);

                    // Son güncelleme zamaný
                    LastUpdateText.Text = $"{DateTime.Now:HH:mm:ss}";

                    _Counter = (_Counter + 1) % 256;
                    DataCountText.Text = _Counter.ToString();

                    System.Diagnostics.Debug.WriteLine($"sitPage telemetri ve GPS güncellendi - Roket Ýrtifa: {telemetryData.RocketAltitude:F2}m, Payload Ýrtifa: {telemetryData.PayloadAltitude:F2}m");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"sitPage telemetri güncelleme hatasý: {ex.Message}");
                    LastUpdateText.Text = $"Hata: {DateTime.Now:HH:mm:ss}";
                }
            });
        }

      

        private void SendDataToCharts(SerialPortService.TelemetryUpdateData telemetryData)
        {
            try
            {
                // SerialPortService üzerinden chart güncelleme yap
                SerialPortService.UpdateChartsFromExternalData(
                    telemetryData.RocketAltitude,     // Roket altitude
                    telemetryData.PayloadAltitude,    // Payload altitude
                    telemetryData.AccelZ,             // Roket AccelZ
                    telemetryData.AccelZ,             // Payload AccelZ
                    telemetryData.RocketSpeed,        // Roket speed
                    telemetryData.PayloadSpeed,       // Payload speed
                    telemetryData.RocketTemperature,  // Roket temperature
                    telemetryData.PayloadTemperature, // Payload temperature
                    telemetryData.RocketPressure,     // Roket pressure
                    telemetryData.PayloadPressure,    // Payload pressure
                    telemetryData.PayloadHumidity,    // Payload humidity
                    "sitPage"                       // Source
                );
                
                System.Diagnostics.Debug.WriteLine($"sitPage TÜM VERÝLER SerialPortService üzerinden chart'lara gönderildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"sitPage chart güncelleme hatasý: {ex.Message}");
            }
        }

        private void UpdateStatistics(SerialPortService.TelemetryUpdateData telemetryData)
        {
            try
            {
                // Maksimum irtifa hesapla
                float currentMaxAltitude = Math.Max(telemetryData.RocketAltitude, telemetryData.PayloadAltitude);
                if (currentMaxAltitude > _maxAltitude)
                {
                    _maxAltitude = currentMaxAltitude;
                    MaxAltitudeText.Text = $"{_maxAltitude:F2} m";
                }


                if (telemetryData.CRC >= 0) 
                {
                    _CRC = telemetryData.CRC;
                    CRCText.Text = _CRC.ToString();
                }

                // Team ID deðerini güncelle
                if (telemetryData.TeamID > 0) 
                {
                    _TeamID = telemetryData.TeamID;
                    TeamIDText.Text = _TeamID.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ýstatistik güncelleme hatasý: {ex.Message}");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // SerialPortService'den ViewModel'i al
            if (SerialPortService.ViewModel != null)
            {
                _viewModel = SerialPortService.ViewModel;
                System.Diagnostics.Debug.WriteLine("sitPage: ChartViewModel baðlandý");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("sitPage: ChartViewModel bulunamadý!");
            }
            
            System.Diagnostics.Debug.WriteLine("sitPage navigasyon tamamlandý - GPS harita sistemi hazýr");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            // Event handler'larý kaldýr
            SerialPortService.OnTelemetryDataUpdated -= OnTelemetryDataUpdated;
            SerialPortService.OnDataReceived -= OnSerialDataReceived;
            SerialPortService.OnRotationDataReceived -= OnRotationDataReceived;
            System.Diagnostics.Debug.WriteLine("sitPage'den ayrýldý - Event handler'lar kaldýrýldý");
        }
    }
}
