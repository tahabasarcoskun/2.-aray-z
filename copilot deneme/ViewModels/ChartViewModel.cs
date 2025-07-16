using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace copilot_deneme.ViewModels
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        private const int MaxDataPoints = 100; // Grafikte gösterilecek maksimum nokta

        private readonly ObservableCollection<ObservableValue> _rocketAltitudeValue;
        private readonly ObservableCollection<ObservableValue> _payloadAltitudeValue;
        private readonly ObservableCollection<ObservableValue> _rocketAccelZValue;
        private readonly ObservableCollection<ObservableValue> _payloadAccelZValue;
        private readonly ObservableCollection<ObservableValue> _rocketSpeedValue;
        private readonly ObservableCollection<ObservableValue> _payloadSpeedValue;
        private readonly ObservableCollection<ObservableValue> _rocketTempValue;
        private readonly ObservableCollection<ObservableValue> _payloadTempValue;
        private readonly ObservableCollection<ObservableValue> _rocketPressureValue;
        private readonly ObservableCollection<ObservableValue> _payloadPressureValue;
        private readonly ObservableCollection<ObservableValue> _payloadHumidityValue;

        // Yeni seriler - jiroskop ve açý için
        private readonly ObservableCollection<ObservableValue> _gyroXValue;
        private readonly ObservableCollection<ObservableValue> _gyroYValue;
        private readonly ObservableCollection<ObservableValue> _gyroZValue;
        private readonly ObservableCollection<ObservableValue> _accelXValue;
        private readonly ObservableCollection<ObservableValue> _accelYValue;
        private readonly ObservableCollection<ObservableValue> _angleValue;

        private string _statusText = "Baðlantý bekleniyor...";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ObservableCollection<ISeries> AltitudeSeries { get; set; }
        public ObservableCollection<ISeries> AccelerationSeries { get; set; }
        public ObservableCollection<ISeries> SpeedSeries { get; set; }
        public ObservableCollection<ISeries> TemperatureSeries { get; set; }
        public ObservableCollection<ISeries> PressureSeries { get; set; }
        public ObservableCollection<ISeries> HumiditySeries { get; set; }
        public ObservableCollection<ISeries> GyroSeries { get; set; }
        public ObservableCollection<ISeries> AngleSeries { get; set; }

        public ChartViewModel()
        {

            _rocketAltitudeValue = new ObservableCollection<ObservableValue>();
            _payloadAltitudeValue = new ObservableCollection<ObservableValue>();

            _rocketAccelZValue = new ObservableCollection<ObservableValue>();
            _payloadAccelZValue = new ObservableCollection<ObservableValue>();

            _rocketSpeedValue = new ObservableCollection<ObservableValue>();
            _payloadSpeedValue = new ObservableCollection<ObservableValue>();

            _rocketTempValue = new ObservableCollection<ObservableValue>();
            _payloadTempValue = new ObservableCollection<ObservableValue>();

            _rocketPressureValue = new ObservableCollection<ObservableValue>();
            _payloadPressureValue = new ObservableCollection<ObservableValue>();

            _payloadHumidityValue = new ObservableCollection<ObservableValue>();

            // Yeni seriler - jiroskop ve açý
            _gyroXValue = new ObservableCollection<ObservableValue>();
            _gyroYValue = new ObservableCollection<ObservableValue>();
            _gyroZValue = new ObservableCollection<ObservableValue>();
            _accelXValue = new ObservableCollection<ObservableValue>();
            _accelYValue = new ObservableCollection<ObservableValue>();
            _angleValue = new ObservableCollection<ObservableValue>();

            AltitudeSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Ýrtifa",
                    Values = _rocketAltitudeValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Ýrtifa",
                    Values = _payloadAltitudeValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            AccelerationSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Z Ývmesi",
                    Values = _rocketAccelZValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Z Ývmesi",
                    Values = _payloadAccelZValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
            };

            SpeedSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Hýz",
                    Values = _rocketSpeedValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Hýz",
                    Values = _payloadSpeedValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            TemperatureSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Sýcaklýk",
                    Values = _rocketTempValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Sýcaklýk",
                    Values = _payloadTempValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            PressureSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Basýnç",
                    Values = _rocketPressureValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Basýnç",
                    Values = _payloadPressureValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            HumiditySeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Nem",
                    Values = _payloadHumidityValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            // Yeni seriler - jiroskop
            GyroSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Jiroskop X",
                    Values = _gyroXValue,
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Jiroskop Y",
                    Values = _gyroYValue,
                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Jiroskop Z",
                    Values = _gyroZValue,
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                    Fill = null, GeometrySize = 0
                }
            };

            // Açý serisi
            AngleSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Açý",
                    Values = _angleValue,
                    Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };
        }

        public void AddRocketAltitudeValue(float value)
        {
            _rocketAltitudeValue.Add(new ObservableValue(value));
            if (_rocketAltitudeValue.Count > MaxDataPoints) _rocketAltitudeValue.RemoveAt(0);
        }
        public void addPayloadAltitudeValue(float value)
        {
            _payloadAltitudeValue.Add(new ObservableValue(value));
            if (_payloadAltitudeValue.Count > MaxDataPoints) _payloadAltitudeValue.RemoveAt(0);
        }
        public void addRocketAccelZValue(float value)
        {
            _rocketAccelZValue.Add(new ObservableValue(value));
            if (_rocketAccelZValue.Count > MaxDataPoints) _rocketAccelZValue.RemoveAt(0);
        }
        public void addPayloadAccelZValue(float value)
        {
            _payloadAccelZValue.Add(new ObservableValue(value));
            if (_payloadAccelZValue.Count > MaxDataPoints) _payloadAccelZValue.RemoveAt(0);
        }
        public void addRocketSpeedValue(float value)
        {
            _rocketSpeedValue.Add(new ObservableValue(value));
            if (_rocketSpeedValue.Count > MaxDataPoints) _rocketSpeedValue.RemoveAt(0);
        }
        public void addPayloadSpeedValue(float value)
        {
            _payloadSpeedValue.Add(new ObservableValue(value));
            if (_payloadSpeedValue.Count > MaxDataPoints) _payloadSpeedValue.RemoveAt(0);
        }
        public void addRocketTempValue(float value)
        {
            _rocketTempValue.Add(new ObservableValue(value));
            if (_rocketTempValue.Count > MaxDataPoints) _rocketTempValue.RemoveAt(0);
        }
        public void addPayloadTempValue(float value)
        {
            _payloadTempValue.Add(new ObservableValue(value));
            if (_payloadTempValue.Count > MaxDataPoints) _payloadTempValue.RemoveAt(0);
        }
        public void addRocketPressureValue(float value)
        {
            _rocketPressureValue.Add(new ObservableValue(value));
            if (_rocketPressureValue.Count > MaxDataPoints) _rocketPressureValue.RemoveAt(0);
        }
        public void addPayloadPressureValue(float value)
        {
            _payloadPressureValue.Add(new ObservableValue(value));
            if (_payloadPressureValue.Count > MaxDataPoints) _payloadPressureValue.RemoveAt(0);
        }
        public void addPayloadHumidityValue(float value)
        {
            _payloadHumidityValue.Add(new ObservableValue(value));
            if (_payloadHumidityValue.Count > MaxDataPoints) _payloadHumidityValue.RemoveAt(0);
        }

        // Yeni metotlar - jiroskop ve açý için
        public void addGyroXValue(float value)
        {
            _gyroXValue.Add(new ObservableValue(value));
            if (_gyroXValue.Count > MaxDataPoints) _gyroXValue.RemoveAt(0);
        }
        
        public void addGyroYValue(float value)
        {
            _gyroYValue.Add(new ObservableValue(value));
            if (_gyroYValue.Count > MaxDataPoints) _gyroYValue.RemoveAt(0);
        }
        
        public void addGyroZValue(float value)
        {
            _gyroZValue.Add(new ObservableValue(value));
            if (_gyroZValue.Count > MaxDataPoints) _gyroZValue.RemoveAt(0);
        }
        
        public void addAccelXValue(float value)
        {
            _accelXValue.Add(new ObservableValue(value));
            if (_accelXValue.Count > MaxDataPoints) _accelXValue.RemoveAt(0);
        }
        
        public void addAccelYValue(float value)
        {
            _accelYValue.Add(new ObservableValue(value));
            if (_accelYValue.Count > MaxDataPoints) _accelYValue.RemoveAt(0);
        }
        
        public void addAngleValue(float value)
        {
            _angleValue.Add(new ObservableValue(value));
            if (_angleValue.Count > MaxDataPoints) _angleValue.RemoveAt(0);
        }

        public void UpdateStatus(string status)
        {
            StatusText = status;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}