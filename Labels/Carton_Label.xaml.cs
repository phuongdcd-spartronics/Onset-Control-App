using Generac_Kodiak_Serialization.Code2D;
using Onset_Serialization.Code2D;
using Onset_Serialization.Models;
using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.Labels
{
    /// <summary>
    /// Interaction logic for Carton_Label.xaml
    /// </summary>
    public partial class Carton_Label : Window, IPrintable
    {
        public Carton_Label()
        {
            InitializeComponent();
        }

        public void Print(LabelInfo info)
        {
            string printerName = Properties.Settings.Default.Carton_Printer;
            tbPO.SetValue(TextBlock.TextProperty, info.PO);
            tbPart.SetValue(TextBlock.TextProperty, info.ProductName);
            tbSerial.SetValue(TextBlock.TextProperty, info.SerialNumber);
            tbQty.SetValue(TextBlock.TextProperty, info.Quantity);

            // call print function
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    imgQRSerial.Source = QRCodeImage.GetQRCodeImage(info.SerialNumber);
                    PrintDialog printer = new PrintDialog();
                    if (!String.IsNullOrEmpty(printerName))
                    {
                        printer.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);
                    }
                    // print on default printer
                    printer.PrintVisual(this.printArea, $"{info.SerialNumber} - Carton");
                }
                catch
                {
                    MessageBox.Show("Print error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
