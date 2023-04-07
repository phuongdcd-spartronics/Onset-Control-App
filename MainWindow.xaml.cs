using OfficeOpenXml;
using Onset_Serialization.Data;
using Onset_Serialization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace Onset_Serialization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private IDictionary<string, Page> _navigateDict = new Dictionary<string, Page>();

        public MainWindow()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitialMenu();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var page = frameContent.Content as Page;
            if (page != null)
            {
                FitFrameSize(page.ClipToBounds);
            }
        }

        private void InitialMenu()
        {
            string[] availItems = new string[]
            {
                nviLogin.Name
            };

            foreach (RadNavigationViewItem item in navMenu.Items)
            {
                if (availItems.Contains(item.Name))
                {
                    item.Visibility = Visibility.Visible;
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void GrantAccess(string id, bool isAdmin = false)
        {
            List<string> functions = _dbContext.AppAuthorizations
                .Where(x => x.Identifier == id)
                .Select(x => x.FunctionName)
                .ToList();

            try
            {
                VisibleMenuItem(navMenu.Items, functions, isAdmin);
            }
            catch (Exception ex)
            {
                CstMessageBox.Show("Grant Error!", ex.Message, CstMessageBoxIcon.Error);
            }
            nviLogin.Visibility = Visibility.Collapsed;
            nviLogOut.Visibility = Visibility.Visible;
        }

        private bool VisibleMenuItem(ItemCollection items, List<string> availFunctions, bool allowAll = false)
        {
            int enabledCount = 0;
            foreach (RadNavigationViewItem item in items)
            {
                string fn = item.CommandParameter?.ToString();
                var subItems = item.Items as ItemCollection;
                bool enabled = false;
                // Browse for all sub items to enable it
                if (subItems != null && subItems.Count > 0)
                {
                    enabled = VisibleMenuItem(subItems, availFunctions, allowAll);
                }
                // This item is enabled
                if (!String.IsNullOrEmpty(fn) && availFunctions.Contains(fn))
                {
                    enabled = true;
                }
                enabled |= allowAll;
                // Visible item
                item.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                enabledCount += enabled ? 1 : 0;
            }
            return enabledCount > 0;
        }

        private void Navigate(string name, string pageName, bool forcedNew = false)
        {
            string ns = this.GetType().Namespace;
            var pageCls = $"{ns}.Pages.{pageName}";
            var page = (Page)Activator.CreateInstance(Type.GetType(pageCls));
            Navigate(name, page, forcedNew);
        }

        private void Navigate(string name, Page page, bool forcedNew = false)
        {
            if (forcedNew && _navigateDict.ContainsKey(name))
            {
                _navigateDict.Remove(name);
            }

            if (_navigateDict.ContainsKey(name))
            {
                frameContent.Content = _navigateDict[name];
            }
            else
            {
                _navigateDict.Add(name, page);
                frameContent.Content = page;
            }

            FitFrameSize(page.ClipToBounds);
            tbPage.Text = name;
        }

        private void FitFrameSize(bool fitSize)
        {
            double width = Double.NaN;
            double height = Double.NaN;

            if (fitSize)
            {
                var parent = frameContent.GetVisualParent<ScrollContentPresenter>();
                // Get the size of scroll content element
                if (parent != null)
                {
                    width = parent.ActualWidth;
                    height = parent.ActualHeight;
                }
            }

            frameContent.Width = width;
            frameContent.Height = height;
        }

        private void nviLogin_Click(object sender, RoutedEventArgs e)
        {
            // Login before using application
            AuthWindow auth = new AuthWindow();
            auth.ShowDialog();
            if (auth.vm.IsLogged)
            {
                UserSession.Create(auth.vm.UserName,
                    auth.vm.Name,
                    auth.vm.Roles);
                GrantAccess(auth.vm.Identifier, auth.vm.Roles.Contains("admin"));
                tbWelcome.Text = $"Welcome, {UserSession.Name}";
                nviLogin.Visibility = Visibility.Collapsed;
            }
        }

        private void navItemToPage_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as RadNavigationViewItem;
            string pageName = item?.CommandParameter?.ToString();
            string name = item?.Content?.ToString() ?? "Unknown";
            if (!String.IsNullOrEmpty(pageName))
            {
                var parent = (item.Parent as NavigationViewSubItemsHost)?.TemplatedParent as RadNavigationViewItem;
                if (parent != null)
                {
                    name = $"{parent.Content} ‣ {name}";
                }
                Navigate(name, pageName);
            }
        }

        private void nviLogOut_Click(object sender, RoutedEventArgs e)
        {
            if (CstMessageBox.Confirm("Log Out", "Đăng xuất khỏi hệ thống. Tiếp tục?"))
            {
                // Clear logged user information
                UserSession.Clear();
                // Refresh main menu
                InitialMenu();
                // Clear all pages cached
                _navigateDict.Clear();
                // Clear frame UI
                frameContent.Content = null;
            }
        }

        private void glRefersh_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CstMessageBox.Confirm("Refresh", "Dữ liệu trên trang hiện tại sẽ bị làm mới. Tiếp tục?"))
            {
                string name = tbPage.Text;
                if (_navigateDict.ContainsKey(name))
                {
                    var page = _navigateDict[name];
                    _navigateDict.Remove(name);
                    Navigate(name, page.GetType().Name);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CstMessageBox.Confirm("Quit", "Thoát khỏi ứng dụng. Tiếp tục?"))
            {
                e.Cancel = true;
            }
        }
    }
}
