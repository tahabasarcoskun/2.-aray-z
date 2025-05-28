using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Defaults;

namespace copilot_deneme.ViewModels
{
    public class ChartViewModels
    {
        // Define properties, methods, and logic for the ViewModel here  
        // Example property:  
        public string ExampleProperty { get; set; } = "Default Value";
    }
    public class ChartViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableValue> _ldrValues = new();
        private ObservableCollection<ObservableValue> _distanceValues = new();
        public ObservableCollection<ISeries> LdrSeries { get; set; }
        public ObservableCollection<ISeries> DistanceSeries { get; set; }
        public string PortInfoText { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ChartViewModel()
        {
            LdrSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _ldrValues,
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Name = "LDR",
                    Fill = null
                }
            };

            DistanceSeries = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _distanceValues,
                    GeometrySize = 0,
                    LineSmoothness = 1,
                    Name = "Distance",
                    Fill = null
                }
            };

            UpdatePortInfo();
        }

        public void AddLdrValue(double value)
        {
            _ldrValues.Add(new ObservableValue(value));
            if (_ldrValues.Count > 50) _ldrValues.RemoveAt(0);
        }

        public void AddDistanceValue(double value)
        {
            _distanceValues.Add(new ObservableValue(value));
            if (_distanceValues.Count > 50) _distanceValues.RemoveAt(0);
        }

        private void UpdatePortInfo()
        {
            var serialPort = SerialPortService.SerialPort;
            PortInfoText = serialPort != null
                ? $"Port: {serialPort.PortName} | Baudrate: {serialPort.BaudRate}"
                : "Port: - | Baudrate: -";
            OnPropertyChanged(nameof(PortInfoText));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}