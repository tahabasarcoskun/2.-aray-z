using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace copilot_deneme
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestPage : Page
    {
        public TestPage()
        {
            InitializeComponent();
        }
        private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            Type pageType;

            switch (currentSelectedIndex)
            {
                case 0:
                    pageType = typeof(sitPage);
                    break;
                case 1:
                    pageType = typeof(sutPage);
                    break;
                default:
                    return;
            }

            // Frame içinde sayfa geçiþini gerçekleþtir
            ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo());
        }

    }
}
