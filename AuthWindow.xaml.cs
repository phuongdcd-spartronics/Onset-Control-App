using Onset_Serialization.Api;
using Onset_Serialization.Api.Models;
using Onset_Serialization.Data;
using Onset_Serialization.Utilities;
using Onset_Serialization.ViewModels;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Onset_Serialization
{
    /// <summary>
    /// Interaction logic for AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private RadCallout _calloutCapslock = null;
        private CacheUtils _cache = new CacheUtils("Authen");

        public AuthViewModel vm { get; private set; } = new AuthViewModel();

        public AuthWindow()
        {
            InitializeComponent();

            this.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            _calloutCapslock = new RadCallout()
            {
                Content = "Capslock is on",
                UseLayoutRounding = true,

                ArrowType = CalloutArrowType.Triangle,
                ArrowAnchorPoint = new Point(0.5, -0.12),
                ArrowBasePoint1 = new Point(0.33, 0.5),
                ArrowBasePoint2 = new Point(0.67, 0.5)
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = vm;
            vm.UserName = _cache.GetCache("_username_")?.ToString();
            txtUsername.Focus();
        }

        private void ToggleCallout()
        {
            if (Keyboard.IsKeyToggled(Key.CapsLock))
            {
                CalloutPopupSettings calloutSettings = new CalloutPopupSettings()
                {
                    AutoClose = true,
                    ShowAnimationType = CalloutAnimation.FadeAndReveal,
                    ShowAnimationDuration = 0.5,
                    CloseAnimationDuration = 0.3,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                };
                CalloutPopupService.Show(_calloutCapslock, txtPassword, calloutSettings);
            }
            else
            {
                CalloutPopupService.Close(_calloutCapslock);
            }
        }

        private void txtPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            CalloutPopupService.Close(_calloutCapslock);
        }

        private void txtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            ToggleCallout();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void TextAuthorized_MouseDown(object sender, MouseButtonEventArgs e)
        {
            vm.UserName = Environment.MachineName;
            txtPassword.Clear();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string machine = Environment.MachineName;

            // Login by computer name (for Operator only), no password required
            if (vm.UserName == machine)
            {
                User user = await UpdateUserAsync(new UserData()
                {
                    UserName = vm.UserName,
                    FullName = "Operator",
                    Department = "Production",
                    Role = "operator"
                });
                vm.IsLogged = true;
                vm.UserName = user.UserName;
                vm.Name = user.FullName;
                vm.Identifier = machine;
                vm.Roles = user.UserRole.Split(';').ToList();
            }
            else
            {
                try
                {
                    idcLoader.IsBusy = true;
                    UserApi api = new UserApi();
                    UserData userData = await api.Authenticate(txtUsername.Text, txtPassword.Password);
                    User user = await UpdateUserAsync(userData);
                    vm.IsLogged = true;
                    vm.Identifier = user.UserName;
                    vm.Name = user.FullName;
                    vm.Roles = user.UserRole.Split(';').ToList();
                }
                catch (Exception ex)
                {
                    CstMessageBox.Show("Login failed", ex.Message, CstMessageBoxIcon.Error);
                }
                finally
                {
                    idcLoader.IsBusy = false;
                }
            }

            if (vm.IsLogged)
            {
                _cache.SetCache("_username_", vm.Identifier);
                _cache.Save();
                DialogResult = true;
            }
        }

        private void txtPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.CapsLock)
            {
                ToggleCallout();
            }
        }

        private async Task<User> UpdateUserAsync(UserData data)
        {
            User user = _dbContext.Users.FirstOrDefault(x => x.UserName == data.UserName);
            if (user == null)
            {
                user = new User()
                { 
                    UserName = data.UserName,
                    UserRole = data.Role,
                    CreatedAt = DateTime.Now,
                    AuthType = data.UserName == Environment.MachineName ? 2 : 1
                };
            }

            user.FullName = data.FullName;
            user.Section = data.Department;
            user.Activated = true;
            _dbContext.Users.AddOrUpdate(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }
    }
}
