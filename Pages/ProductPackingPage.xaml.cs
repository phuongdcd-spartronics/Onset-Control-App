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
    /// Interaction logic for ProductPackingPage.xaml
    /// </summary>
    public partial class ProductPackingPage : Page
    {
        private const int STATION_INDEX = Station.BOX;

        private readonly OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ScanViewModel _vm = new ScanViewModel();

        public ProductPackingPage()
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

        private async void txtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    string data = txtScan.Text;
                    txtScan.Clear();
                    tbMessage.Foreground = Brushes.Red;

                    var wo = cbbWO.SelectedItem as ProductionOrder;
                    SerialRouting route = await _dbContext.SerialRoutings
                        .Where(x => x.SerialNumber == data && x.StationIndex == STATION_INDEX)
                        .FirstOrDefaultAsync();
                    SerialRouting prevRoute = await _dbContext.SerialRoutings
                        .Where(x => x.SerialNumber == data && x.StationIndex < STATION_INDEX)
                        .OrderByDescending(x => x.StationIndex)
                        .FirstOrDefaultAsync();
                    if (route == null)
                    {
                        tbMessage.Text = $"`{data}` không có trạm này!";
                        return;
                    }
                    else if (route.OrderId != wo.Id)
                    {
                        tbMessage.Text = $"`{route.SerialNumber}` không thuộc `{wo.Number}`";
                        return;
                    }
                    else if (route.Status == Status.PASSED)
                    {
                        tbMessage.Text = $"`{data}` đã sử dụng tại trạm này!";
                        return;
                    }
                    else if (prevRoute != null && prevRoute.Status != Status.PASSED)
                    {
                        tbMessage.Text = $"`{prevRoute.SerialNumber}` chưa qua trạm `{prevRoute.StationName}`!";
                        return;
                    }

                    _vm.Append(data);

                    using (var transaction = _dbContext.Database.BeginTransaction())
                    {
                        // Initialize and record printed labels
                        var labels = await _dbContext.ProductLabels
                            .Where(x => x.ProductId == wo.ProductId && x.StationIndex == STATION_INDEX)
                            .ToListAsync();
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
                        await _dbContext.SaveChangesAsync();
                        transaction.Commit();

                        // Print label
                        foreach (var command in printActions)
                        {
                            command.Invoke();
                        }

                        // Clear UI;
                        txtScan.Clear();
                        _vm.Clear();
                        tbMessage.Foreground = Brushes.Green;
                        tbMessage.Text = "Successful!";
                    }
                }
                catch (Exception ex)
                {
                    tbMessage.Text = ex.Message;
                }
            }
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
            }
        }
    }
}
