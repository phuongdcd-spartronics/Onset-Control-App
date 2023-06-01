using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using Onset_Serialization.Models;
using Onset_Serialization.Utilities;
using Onset_Serialization.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for SerialInitialPage.xaml
    /// </summary>
    public partial class SerialPrintPage : Page
    {
        private const int STATION_INDEX = Station.INITIALIZE;

        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ScanViewModel _vm = new ScanViewModel();

        public SerialPrintPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = _vm;
            this.cbbWO.ItemsSource = _dbContext.ProductionOrders
                .Where(x => x.Status == Status.STARTED)
                .ToList();
            this.cbbWO.DisplayMemberPath = "Number";
        }

        private void cbbWO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var order = cbbWO.SelectedItem as ProductionOrder;
            if (order != null)
            {
                _vm = new ScanViewModel(_dbContext.RouterInputs.Where(x => x.ProductRouter.ProductId == order.ProductId && x.ProductRouter.StationIndex == STATION_INDEX).ToList());
                this.DataContext = _vm;
                tbMessage.Foreground = Brushes.Orange;
                tbMessage.Text = $"Đã cập nhật thông tin sản phẩm {order.Product.Name}";

                SerialRouting serialRouting = _dbContext.SerialRoutings.Where(x => x.OrderId == order.Id && x.StationIndex == STATION_INDEX && x.Status == "Passed").OrderByDescending(x => x.SerialNumber).FirstOrDefault();
                if (serialRouting != null)
                    txtLastPrinted.Text = serialRouting.SerialNumber;
            }
        }

        private void btPrintLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbMessage.Foreground = Brushes.Red;

                var wo = cbbWO.SelectedItem as ProductionOrder;
                SerialRouting route = _dbContext.SerialRoutings
                    .Where(x => x.OrderId == wo.Id && x.StationIndex == STATION_INDEX && x.Status == Status.CREATED)
                    .OrderBy(x => x.SerialNumber)
                    .FirstOrDefault();

                if (route == null)
                {
                    tbMessage.Text = $"Đã hết serial number cho `{wo.Number}`!";
                    return;
                }

                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    // Initialize and record printed labels
                    var labels = _dbContext.ProductLabels
                        .Where(x => x.ProductId == wo.ProductId && x.StationIndex == STATION_INDEX)
                        .ToList();
                    List<Action> printActions = new List<Action>();
                    foreach (var label in labels)
                    {
                        LabelInfo info = PrintUtils.GetLabelInfo(label.LabelName, route);
                        printActions.Add(new Action(() =>
                        {
                            PrintUtils.PrintLabel(label.LabelName, info);
                        }));
                        _dbContext.PrintLogs.Add(new PrintLog()
                        {
                            RefId = route.Id,
                            SerialNumber = route.SerialNumber,
                            LabelName = label.LabelName,
                            PrintedData = JsonUtils.Serialize(info),
                            PrintedBy = UserSession.UserID,
                            PrintedDate = DateTime.Now,
                            Machine = Environment.MachineName
                        });
                    }

                    // Commit changes on database
                    route.Status = Status.PASSED;
                    route.ModifiedBy = UserSession.UserID;
                    route.ModifiedAt = DateTime.Now;
                    _dbContext.SaveChanges();
                    transaction.Commit();

                    // Print label
                    foreach (var command in printActions)
                    {
                        command.Invoke();
                    }

                    // Clear UI
                    _vm.Clear();
                    tbMessage.Foreground = Brushes.Green;
                    tbMessage.Text = "Label has been printed successfully";
                    txtLastPrinted.Text = route.SerialNumber;
                }
            }
            catch (Exception ex)
            {
                tbMessage.Text = ex.Message;
            }
        }
    }
}
