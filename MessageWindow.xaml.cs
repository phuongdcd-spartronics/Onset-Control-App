using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Onset_Serialization
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public const int MESSAGE_TYPE_INFO = 1;
        public const int MESSAGE_TYPE_CONFIRM = 2;

        public MessageWindow(string title, string message)
        {
            InitializeComponent();

            this.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            txtTitle.Text = title;
            txtMessage.Text = message;
        }

        public MessageWindow(string title, string message, CstMessageBoxIcon icon) : this(title, message)
        {
            switch (icon)
            {
                case CstMessageBoxIcon.Success:
                    txtTitle.Foreground = Brushes.Green;
                    imgIcon.Source = new BitmapImage(new System.Uri("/Images/success-icon.png", System.UriKind.Relative));
                    break;
                case CstMessageBoxIcon.Error:
                    txtTitle.Foreground = Brushes.Red;
                    imgIcon.Source = new BitmapImage(new System.Uri("/Images/error-icon.png", System.UriKind.Relative));
                    break;
                case CstMessageBoxIcon.Warning:
                    txtTitle.Foreground = Brushes.Gold;
                    imgIcon.Source = new BitmapImage(new System.Uri("/Images/warning-icon.png", System.UriKind.Relative));
                    break;
                case CstMessageBoxIcon.Message:
                    txtTitle.Foreground = Brushes.CornflowerBlue;
                    imgIcon.Source = new BitmapImage(new System.Uri("/Images/envelope-icon.png", System.UriKind.Relative));
                    break;
                case CstMessageBoxIcon.Question:
                    txtTitle.Foreground = Brushes.Goldenrod;
                    imgIcon.Source = new BitmapImage(new System.Uri("/Images/question-icon.png", System.UriKind.Relative));
                    break;
            }
        }

        public void SetMessageType(int type)
        {
            switch (type)
            {
                case MESSAGE_TYPE_INFO:
                    gridConfirm.Visibility = Visibility.Visible;
                    break;
                case MESSAGE_TYPE_CONFIRM:
                    gridYesNo.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void txtClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }

    public enum CstMessageBoxIcon
    {
        Success = 0,
        Error = 1,
        Warning = 2,
        Message = 3,
        Question = 4
    }

    public static class CstMessageBox
    {
        public static void Show(string title, string message, CstMessageBoxIcon icon = CstMessageBoxIcon.Message)
        {
            MessageWindow window = new MessageWindow(title, message, icon);
            window.SetMessageType(MessageWindow.MESSAGE_TYPE_INFO);
            window.ShowDialog();
        }

        public static bool Confirm(string title, string message, CstMessageBoxIcon icon = CstMessageBoxIcon.Question)
        {
            MessageWindow window = new MessageWindow(title, message, icon);
            window.SetMessageType(MessageWindow.MESSAGE_TYPE_CONFIRM);
            return window.ShowDialog() ?? false;
        }
    }
}
