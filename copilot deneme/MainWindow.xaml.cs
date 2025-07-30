using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace copilot_deneme
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Type? _currentPageType; // Mevcut sayfa t�r�n� takip etmek i�in
        private int _currentPageIndex = 0; // Mevcut sayfa indeksini takip etmek i�in
        private DispatcherTimer? _splashTimer; // Splash screen timer'�

        // Navigation s�ras�n� tan�mla
        private readonly Dictionary<Type, int> _pageIndexMap = new Dictionary<Type, int>
        {
            { typeof(HomePage), 0 },
            { typeof(ChartPage), 1 },
            { typeof(SettingPage), 2 },
            { typeof(sitPage), 3 }  
        };

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Activated event'ini dinle (ilk kez window aktif oldu�unda)
            this.Activated += MainWindow_Activated;
            
            // Custom title bar ayarlar�
            SetupCustomTitleBar();
            
            // Splash screen'i ba�lat
            StartSplashScreen();
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Sadece ilk aktivasyon i�in kontrol et
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                // App resource'lar�ndaki g�rsellerin y�klenip y�klenmedi�ini kontrol et
                try
                {
                    var titleBarLogoResource = Application.Current.Resources["TitleBarLogoImage"];
                    var teamLogoResource = Application.Current.Resources["TeamLogoImage"];
                    var titleBarBrushResource = Application.Current.Resources["TitleBarLogoBrush"];
                    var teamBrushResource = Application.Current.Resources["TeamLogoBrush"];
                    
                    System.Diagnostics.Debug.WriteLine($"??? TitleBarLogoImage resource: {(titleBarLogoResource != null ? "? Y�klendi" : "? Bulunamad�")}" );
                    System.Diagnostics.Debug.WriteLine($"??? TeamLogoImage resource: {(teamLogoResource != null ? "? Y�klendi" : "? Bulunamad�")}");
                    System.Diagnostics.Debug.WriteLine($"?? TitleBarLogoBrush resource: {(titleBarBrushResource != null ? "? Y�klendi" : "? Bulunamad�")}");
                    System.Diagnostics.Debug.WriteLine($"?? TeamLogoBrush resource: {(teamBrushResource != null ? "? Y�klendi" : "? Bulunamad�")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Resource kontrol hatas�: {ex.Message}");
                }
            }
        }

        private void StartSplashScreen()
        {
            // Splash overlay'i g�ster, ana i�eri�i gizle
            SplashOverlay.Visibility = Visibility.Visible;
            SplashOverlay.Opacity = 1.0;
            MainContent.Opacity = 0.0;
            
            // 3 saniye timer ba�lat
            _splashTimer = new DispatcherTimer();
            _splashTimer.Interval = TimeSpan.FromSeconds(3);
            _splashTimer.Tick += SplashTimer_Tick;
            _splashTimer.Start();
            
            System.Diagnostics.Debug.WriteLine("?? Splash screen ba�lat�ld� - 3 saniye g�r�necek");
        }

        private void SplashTimer_Tick(object? sender, object e)
        {
            // Timer'� durdur
            _splashTimer?.Stop();
            _splashTimer = null;
            
            // Fade efekti ile ge�i� yap
            StartFadeTransition();
            
            System.Diagnostics.Debug.WriteLine("? Splash screen s�resi doldu - Fade ge�i�i ba�lat�l�yor");
        }

        private void StartFadeTransition()
        {
            // Ana uygulamay� haz�rla
            InitializeMainApp();
            
            // C# kodu ile animasyon olu�tur
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(800)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeOutStoryboard = new Storyboard();
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeOutAnimation, SplashOverlay);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
            
            fadeOutStoryboard.Completed += (s, e) =>
            {
                // Splash overlay'i gizle
                SplashOverlay.Visibility = Visibility.Collapsed;
                
                // Ana i�eri�in fade in animasyonunu ba�lat
                StartMainContentFadeIn();
            };
            
            fadeOutStoryboard.Begin();
            System.Diagnostics.Debug.WriteLine("?? Splash fade out animasyonu ba�lat�ld�");
        }

        private void StartMainContentFadeIn()
        {
            // Ana i�eri�i g�r�n�r yap (hen�z opacity 0)
            MainContent.Visibility = Visibility.Visible;
            
            // C# kodu ile fade in animasyonu olu�tur
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            var fadeInStoryboard = new Storyboard();
            fadeInStoryboard.Children.Add(fadeInAnimation);
            Storyboard.SetTarget(fadeInAnimation, MainContent);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            
            fadeInStoryboard.Completed += (s, e) =>
            {
                // Ana i�erik g�r�n�r olduktan sonra UI'yi yenile
                DispatcherQueue.TryEnqueue(() =>
                {
                    // NavigationView'� force refresh et
                    NavigationView.InvalidateArrange();
                    NavigationView.InvalidateMeasure();
                    
                    // AppTitleBar'� force refresh et
                    AppTitleBar.InvalidateArrange();
                    AppTitleBar.InvalidateMeasure();
                    
                    System.Diagnostics.Debug.WriteLine("? Ana i�erik fade in animasyonu tamamland� - UI yenilendi");
                });
            };
            
            fadeInStoryboard.Begin();
            System.Diagnostics.Debug.WriteLine("?? Ana i�erik fade in animasyonu ba�lat�ld�");
        }

        private void InitializeMainApp()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Giri� ekran�n� atla, do�rudan HomePage'e git
                _currentPageType = typeof(HomePage);
                _currentPageIndex = _pageIndexMap[_currentPageType];
                ContentFrame.Navigate(_currentPageType);
                
                // NavigationView'da ilk ��eyi se�ili yap
                if (NavigationView.MenuItems.Count > 0)
                {
                    NavigationView.SelectedItem = NavigationView.MenuItems[0];
                }
            });
        }

        private void SetupCustomTitleBar()
        {
            try
            {
                // Window handle'�n� al
                var hWnd = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    // Title bar'� �zelle�tir
                    appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    appWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 15, 15, 35);
                    appWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    appWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 15, 15, 35);
                    appWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 128, 128, 128);
                    appWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 255, 217, 61);
                    appWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                    appWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 199, 206, 234);
                    appWindow.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);

                    // Draggable region ayarla
                    appWindow.TitleBar.SetDragRectangles(new Windows.Graphics.RectInt32[] 
                    { 
                        new Windows.Graphics.RectInt32 { X = 0, Y = 0, Width = 10000, Height = 48 } 
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Custom title bar setup error: {ex.Message}");
            }
        }

        private NavigationTransitionInfo GetNavigationTransition(int currentIndex, int targetIndex)
        {
            if (targetIndex > currentIndex)
            {
                // Sa�a do�ru kayma (liste s�ras�nda a�a��da) - FromRight kullan
                return new SlideNavigationTransitionInfo()
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                };
            }
            else if (targetIndex < currentIndex)
            {
                // Sola do�ru kayma (liste s�ras�nda yukar�da) - FromLeft kullan
                return new SlideNavigationTransitionInfo()
                {
                    Effect = SlideNavigationTransitionEffect.FromLeft
                };
            }
            else
            {
                // Ayn� sayfa - varsay�lan ge�i�
                return new EntranceNavigationTransitionInfo();
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                Type? targetPageType = null;

                switch (selectedItem.Tag)
                {
                    case "home":
                        targetPageType = typeof(HomePage);
                        break;
                    
                    case "profile":
                        targetPageType = typeof(ChartPage);
                        break;
                    
                    case "settings":
                        targetPageType = typeof(SettingPage);
                        break;
                    
                    case "test":
                        targetPageType = typeof(sitPage);
                        break;
                }

                // Sadece farkl� bir sayfa se�ildiyse navigate et
                if (targetPageType != null && targetPageType != _currentPageType)
                {
                    var targetIndex = _pageIndexMap[targetPageType];
                    var transition = GetNavigationTransition(_currentPageIndex, targetIndex);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ContentFrame.Navigate(targetPageType, null, transition);
                        _currentPageType = targetPageType;
                        _currentPageIndex = targetIndex;
                        
                        string direction = targetIndex > _currentPageIndex ? "?? Sa�a (FromRight)" : "?? Sola (FromLeft)";
                        System.Diagnostics.Debug.WriteLine($"?? Navigated to: {targetPageType.Name}");
                        System.Diagnostics.Debug.WriteLine($"?? Transition: {direction}");
                        System.Diagnostics.Debug.WriteLine($"?? Index: {_currentPageIndex} ? {targetIndex}");
                    });
                }
                else if (targetPageType == _currentPageType)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Already on {targetPageType?.Name}, navigation prevented");
                }
            }
        }
    }
}
