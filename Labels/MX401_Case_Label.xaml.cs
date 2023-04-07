using Onset_Serialization.Code2D;
using Onset_Serialization.Models;
using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.Labels
{
    /// <summary>
    /// Interaction logic for MX1101_Case_Label.xaml
    /// </summary>
    public partial class MX401_Case_Label : Window, IPrintable
    {
        public MX401_Case_Label()
        {
            InitializeComponent();
        }

        public void Print(LabelInfo info)
        {
            string printerName = Properties.Settings.Default.Case_Printer;
            tbSerial.SetValue(TextBlock.TextProperty, info.SerialNumber);

            // call print function
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var code = DataMatrixImage.GetDataMatrixCodeImage(info.SerialNumber);
                    imgDMSerial.Source = code;

                    PrintDialog printer = new PrintDialog();
                    if (!String.IsNullOrEmpty(printerName))
                    {
                        printer.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);
                    }
                    // print on default printer
                    printer.PrintVisual(this.printArea, $"{info.SerialNumber} - MX401 Case");
                }
                catch
                {
                    MessageBox.Show("Print error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
