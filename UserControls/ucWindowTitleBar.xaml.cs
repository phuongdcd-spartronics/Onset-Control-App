using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Onset_Serialization.UserControls
{
    /// <summary>
    /// Interaction logic for ucWindowTitleBar.xaml
    /// </summary>
    public partial class ucWindowTitleBar : UserControl
    {
        private Window _parent = null;

        public ucWindowTitleBar()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parent = Window.GetWindow(this);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _parent?.DragMove();
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MaximizeWindow();
        }

        private void icoMinimize_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_parent != null)
            {
                _parent.WindowState = WindowState.Minimized;
            }
        }

        private void icoMaximize_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MaximizeWindow();
        }

        private void icoQuit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_parent != null)
            {
                _parent.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void MaximizeWindow()
        {
            if (_parent != null)
            {
                if (_parent.WindowState == WindowState.Maximized)
                {
                    _parent.WindowState = WindowState.Normal;
                }
                else
                {
                    _parent.WindowState = WindowState.Maximized;
                }
            }
        }
    }
}
