using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace copilot_deneme
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AccountPage : Page
    {
        public AccountPage()
        {
            this.InitializeComponent();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var username = UsernameBox.Text;
                var password = PasswordBox.Password;

                bool result = AuthService.SignIn(username, password);

                if (result)
                {
                    if (this.Parent is Frame frame)
                    {
                        frame.Navigate(typeof(HomePage));
                    }
                }
                else
                {
                    ErrorText.Text = "Invalid username or password.";
                }
            });
        }
    }
}