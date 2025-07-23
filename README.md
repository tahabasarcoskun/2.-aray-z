# Rocket Ground Station Telemetry System

A comprehensive Windows application built with WinUI 3 (.NET 8) for real-time rocket and payload telemetry monitoring, data visualization, and GPS tracking.

## ?? Project Structure

```
C:\Users\Emirhan\Desktop\yeristasyonu\
??? copilot deneme\                    # Main project directory
?   ??? App.xaml                       # Application definition
?   ??? MainWindow.xaml               # Main window with navigation
?   ??? HomePage.xaml                 # Home/dashboard page
?   ??? ChartPage.xaml                # Data visualization charts
?   ??? SettingPage.xaml              # Serial port configuration
?   ??? sitPage.xaml                  # SIT telemetry & GPS tracking
?   ??? HYI.xaml                      # HYI telemetry monitoring
?   ??? SerialPortService.cs          # Main serial communication service
?   ??? SerialPortServiceHYI.cs       # HYI-specific serial service
?   ??? ViewModels\ChartViewModel.cs  # Data binding for charts
?   ??? Assets\                       # Application resources
?   ?   ??? saturn_tam.ico           # Application icon
?   ?   ??? teamlogo.png             # Team logo
?   ?   ??? rocket_model.STL         # 3D rocket model
?   ?   ??? line-chart-128.png       # Chart icon
?   ??? copilot deneme.csproj        # Project file
??? README.md                        # This file
```

## ?? Features

### ?? Real-time Telemetry Monitoring
- **Rocket Telemetry**: Altitude, GPS coordinates, speed, temperature, pressure
- **Payload Telemetry**: Altitude, GPS coordinates, speed, temperature, pressure, humidity
- **IMU Data**: Gyroscope (X, Y, Z axes) and accelerometer readings
- **Flight Angle**: Real-time orientation data

### ?? Data Visualization
- **Live Charts**: Real-time plotting of telemetry data
- **Multiple Data Series**: Separate tracking for rocket and payload
- **Chart Types**: Altitude, acceleration, speed, temperature, pressure, humidity
- **Historical Data**: Time-series data storage and display

### ??? GPS Tracking & Mapping
- **Real-time GPS Tracking**: Live position updates for both rocket and payload
- **Interactive Map**: Web-based mapping with Leaflet.js
- **Flight Path Visualization**: Trail tracking for both vehicles
- **Distance Calculation**: Real-time distance between rocket and payload
- **Altitude Display**: 3D position information

### ?? 3D Model Visualization
- **3D Rocket Model**: STL file rendering with Three.js
- **Real-time Orientation**: Live rotation updates based on IMU data
- **Interactive View**: 3D model responds to telemetry orientation data

### ?? Configuration & Settings
- **Serial Port Management**: Dynamic port detection and configuration
- **Baud Rate Settings**: Configurable communication speeds
- **Connection Status**: Real-time connection monitoring
- **Multi-port Support**: Separate input/output port configuration

## ??? Technical Specifications

### Platform & Framework
- **Target Framework**: .NET 8.0 (Windows)
- **UI Framework**: WinUI 3 with Windows App SDK 1.7
- **Minimum Windows Version**: Windows 10 (build 17763)
- **Target Windows Version**: Windows 10 (build 19041)

### Key Dependencies
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250513003" />
<PackageReference Include="CSharpMarkup.WinUI.LiveChartsCore.SkiaSharpView" Version="3.1.0" />
<PackageReference Include="HelixToolkit.WinUI" Version="2.27.0" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3351.48" />
<PackageReference Include="System.IO.Ports" Version="9.0.5" />
<PackageReference Include="SkiaSharp" Version="2.88.9" />
```

### Supported Architectures
- x86
- x64
- ARM64

## ?? Getting Started

### Prerequisites
- Windows 10/11 (version 17763 or later)
- Visual Studio 2022 with:
  - .NET 8.0 SDK
  - Windows App SDK
  - C# and WinUI 3 workload

### Building the Project
1. Clone or download the project to `C:\Users\Emirhan\Desktop\yeristasyonu\`
2. Open `copilot deneme\copilot deneme.csproj` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution (Ctrl+Shift+B)
5. Run the application (F5)

### Serial Port Configuration
1. Navigate to **Settings** page
2. Select appropriate COM ports for input/output
3. Configure baud rate (default options available)
4. Click **Connect** to establish communication
5. Monitor connection status indicators

## ?? Data Flow Architecture

### Serial Communication
```
Hardware ? Serial Port ? SerialPortService ? ViewModels ? UI Components
```

### Telemetry Data Processing
1. **Data Reception**: Raw serial data from rocket/payload systems
2. **Parsing**: Convert serial strings to structured telemetry objects
3. **Distribution**: Send parsed data to multiple UI components
4. **Visualization**: Real-time updates to charts, maps, and 3D models
5. **Storage**: Historical data for analysis and replay

### Multi-page Data Sharing
- **ChartViewModel**: Centralized data model for all telemetry
- **SerialPortService**: Event-driven data distribution
- **Real-time Updates**: Synchronized updates across all pages

## ?? Application Pages

### ?? Home Page
- Dashboard overview
- System status indicators
- Quick navigation

### ?? Chart Page  
- Live telemetry charts
- Multi-series data visualization
- Real-time plotting

### ?? Settings Page
- Serial port configuration
- Connection management
- System settings

### ??? SIT Page
- Comprehensive telemetry display
- GPS mapping integration
- 3D rocket model visualization
- Flight statistics

### ?? HYI Page
- HYI-specific telemetry monitoring
- Dual port management (input/output)
- Specialized data handling

## ?? Telemetry Data Format

### Rocket Data
- Altitude (m)
- GPS Coordinates (latitude, longitude)
- GPS Altitude (m)
- Speed (m/s)
- Temperature (°C)
- Pressure (hPa)

### Payload Data
- Altitude (m)
- GPS Coordinates (latitude, longitude)
- Speed (m/s)
- Temperature (°C)
- Pressure (hPa)
- Humidity (%)

### IMU Data
- Gyroscope: X, Y, Z (°/s)
- Accelerometer: X, Y, Z (m/s²)
- Flight Angle (°)

### System Data
- Packet Counter
- CRC Checksum
- Team ID
- Timestamp

## ?? Customization

### Adding New Telemetry Parameters
1. Update `TelemetryUpdateData` class
2. Modify parsing logic in `SerialPortService`
3. Add UI elements to relevant pages
4. Update chart configurations

### Configuring GPS Map
- Map tiles: OpenStreetMap (configurable)
- Default center: Ankara, Turkey (39.925533, 32.866287)
- Zoom levels: 1-18
- Custom markers and styling

### 3D Model Customization
- Replace `rocket_model.STL` with custom model
- Adjust scaling and positioning in Three.js configuration
- Modify material properties and lighting

## ?? Troubleshooting

### Common Issues

**Serial Port Connection Failed**
- Check COM port availability
- Verify baud rate settings
- Ensure hardware connections
- Try refreshing available ports

**GPS Map Not Loading**
- Check internet connection (required for map tiles)
- Verify WebView2 runtime installation
- Check browser compatibility

**3D Model Not Rendering**
- Verify STL file integrity
- Check Three.js library loading
- Ensure WebView2 functionality

**Charts Not Updating**
- Verify data reception in SerialPortService
- Check ViewModel binding
- Ensure UI thread dispatching

## ?? License

This project is part of a rocket/aerospace telemetry system. Please ensure compliance with local regulations regarding radio communications and aerospace activities.

## ?? Team

**Saturn Rocket Team**
- Ground Station Software Development
- Telemetry Systems Integration
- Real-time Data Visualization

---

*For technical support or questions, please refer to the project documentation or contact the development team.*