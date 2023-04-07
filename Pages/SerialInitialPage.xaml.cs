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
    public partial class SerialInitialPage : Page
    {
        private const int STATION_INDEX = Station.INITIALIZE;

        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ScanViewModel _vm = new ScanViewModel();

        public SerialInitialPage()
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
                    SerialComponent comp = _dbContext.SerialComponents.FirstOrDefault(x => x.ComponentNumber == data);
                    SerialRouting route = await _dbContext.SerialRoutings
                        .Where(x => x.OrderId == wo.Id && x.StationIndex == STATION_INDEX && x.Status == Status.CREATED)
                        .OrderBy(x => x.SerialNumber)
                        .FirstOrDefaultAsync();

                    if (comp != null)
                    {
                        tbMessage.Text = $"`{comp.ComponentNumber}` đã được sử dụng cho `{comp.SerialNumber}`!";
                        return;
                    }
                    else if (route == null)
                    {
                        tbMessage.Text = $"Đã hết serial number cho `{wo.Number}`!";
                        return;
                    }

                    _vm.Append(data);

                    if (_vm.IsFull())
                    {
                        using (var transaction = _dbContext.Database.BeginTransaction())
                        {
                            // Record component information
                            _dbContext.SerialComponents.Add(new SerialComponent()
                            {
                                SerialNumber = route.SerialNumber,
                                ComponentNumber = data,
                                StationIndex = STATION_INDEX,
                                CreatedBy = UserSession.UserID,
                                CreatedAt = DateTime.Now
                            });

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

                            // Clear UI
                            _vm.Clear();
                            txtScan.Clear();
                            tbMessage.Foreground = Brushes.Green;
                            tbMessage.Text = "Successful!";
                        }
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
