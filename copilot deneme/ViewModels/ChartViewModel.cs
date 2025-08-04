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
        private const int MaxDataPoints = 100; // Grafikte g�sterilecek maksimum nokta

        private readonly ObservableCollection<ObservableValue> _rocketAltitudeValue;
        private readonly ObservableCollection<ObservableValue> _payloadAltitudeValue;
        private readonly ObservableCollection<ObservableValue> _accelZValue;
        private readonly ObservableCollection<ObservableValue> _accelXValue;
        private readonly ObservableCollection<ObservableValue> _accelYValue;
        private readonly ObservableCollection<ObservableValue> _rocketSpeedValue;
        private readonly ObservableCollection<ObservableValue> _payloadSpeedValue;
        private readonly ObservableCollection<ObservableValue> _rocketTempValue;
        private readonly ObservableCollection<ObservableValue> _payloadTempValue;
        private readonly ObservableCollection<ObservableValue> _rocketPressureValue;
        private readonly ObservableCollection<ObservableValue> _payloadPressureValue;
        private readonly ObservableCollection<ObservableValue> _payloadHumidityValue;


        private string _statusText = "Ba�lant� bekleniyor...";
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

        public ChartViewModel()
        {

            _rocketAltitudeValue = new ObservableCollection<ObservableValue>();
            _payloadAltitudeValue = new ObservableCollection<ObservableValue>();

            _accelXValue = new ObservableCollection<ObservableValue>();
            _accelYValue = new ObservableCollection<ObservableValue>();
            _accelZValue = new ObservableCollection<ObservableValue>();

            _rocketSpeedValue = new ObservableCollection<ObservableValue>();
            _payloadSpeedValue = new ObservableCollection<ObservableValue>();

            _rocketTempValue = new ObservableCollection<ObservableValue>();
            _payloadTempValue = new ObservableCollection<ObservableValue>();

            _rocketPressureValue = new ObservableCollection<ObservableValue>();
            _payloadPressureValue = new ObservableCollection<ObservableValue>();

            _payloadHumidityValue = new ObservableCollection<ObservableValue>();


            AltitudeSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket �rtifa",
                    Values = _rocketAltitudeValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload �rtifa",
                    Values = _payloadAltitudeValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            AccelerationSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = " X �vmesi",
                    Values = _accelXValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = " Y �vmesi",
                    Values = _accelYValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                 new LineSeries<ObservableValue>
                {
                    Name = " Z �vmesi",
                    Values = _accelZValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
            };

            SpeedSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket H�z",
                    Values = _rocketSpeedValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload H�z",
                    Values = _payloadSpeedValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            TemperatureSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket S�cakl�k",
                    Values = _rocketTempValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload S�cakl�k",
                    Values = _payloadTempValue,
                    Stroke = new SolidColorPaint(SKColors.IndianRed) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                }
            };

            PressureSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Name = "Roket Bas�n�",
                    Values = _rocketPressureValue,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    Fill = null, GeometrySize = 0
                },
                new LineSeries<ObservableValue>
                {
                    Name = "Payload Bas�n�",
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
        public void addRocketAccelXValue(float value)
        {
            _accelXValue.Add(new ObservableValue(value));
            if (_accelXValue.Count > MaxDataPoints) _accelXValue.RemoveAt(0);
        }
        public void addRocketAccelYValue(float value)
        {
            _accelYValue.Add(new ObservableValue(value));
            if (_accelYValue.Count > MaxDataPoints) _accelYValue.RemoveAt(0);
        }
        public void addRocketAccelZValue(float value)
        {
            _accelZValue.Add(new ObservableValue(value));
            if (_accelZValue.Count > MaxDataPoints) _accelZValue.RemoveAt(0);
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