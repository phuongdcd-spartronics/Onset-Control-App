using Onset_Serialization.Code2D;
using Onset_Serialization.Models;
using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.Labels
{
    /// <summary>
    /// Interaction logic for MX2202_Box_Label.xaml
    /// </summary>
    public partial class MX2202_Box_V2_Label : Window, IPrintable
    {
        public MX2202_Box_V2_Label()
        {
            InitializeComponent();
        }

        public void Print(LabelInfo info)
        {
            string printerName = Properties.Settings.Default.Box_Printer;
            tbSerial.SetValue(TextBlock.TextProperty, info.SerialNumber);
            //tbProductName.SetValue(TextBlock.TextProperty, info.ProductName);

            // call print function
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    imgDMSerial.Source = DataMatrixImage.GetDataMatrixCodeImage(info.SerialNumber);

                    PrintDialog printer = new PrintDialog();
                    if (!String.IsNullOrEmpty(printerName))
                    {
                        printer.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);
                    }
                    // print on default printer
                    printer.PrintVisual(this.printArea, $"{info.SerialNumber} - MX2202 Box");
                }
                catch
                {
                    MessageBox.Show("Print error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
