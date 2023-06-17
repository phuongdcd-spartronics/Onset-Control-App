using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using Onset_Serialization.Models;
using Onset_Serialization.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for ProductionOrderPage.xaml
    /// </summary>
    public partial class ProductionOrderPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ProductionOrderViewModel _vm = new ProductionOrderViewModel();

        public ProductionOrderPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.ProductList = _dbContext.Products.ToList();
            _vm.OrderList = new System.Collections.ObjectModel.ObservableCollection<ProductionOrder>();
            _vm.Order = new ProductionOrder();
            this.DataContext = _vm;
            LoadOrders();
        }

        private void LoadOrders()
        {
            _vm.OrderList.Clear();
            var orders = _dbContext.ProductionOrders
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
            foreach (var order in orders)
            {
                _vm.OrderList.Add(order);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.IsNew)
                {
                    if (_dbContext.ProductionOrders.Any(x => x.Number == _vm.Order.Number))
                    {
                        throw new Exception($"{_vm.Order.Number} đã tồn tại!");
                    }
                    _vm.Order.Status = Status.CREATED;
                    _vm.Order.CreatedBy = UserSession.UserID;
                    _vm.Order.CreatedAt = DateTime.Now;
                    _dbContext.ProductionOrders.Add(_vm.Order);
                }
                else if (_vm.OrderList.Any(x => x.Quantity <= 0))
                {
                    throw new Exception($"Số lượng phải là số nguyên dương!");
                }

                int affected = _dbContext.SaveChanges();

                if (_vm.IsNew)
                {
                    _vm.IsNew = false;
                    LoadOrders();
                }

                if (affected > 0)
                {
                    CstMessageBox.Show("Success", $"Lưu thành công {affected} thay đổi!", CstMessageBoxIcon.Success);
                }
            }
            catch (Exception ex)
            {
                CstMessageBox.Show("Error", ex.Message, CstMessageBoxIcon.Error);
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            List<SerialData> serialList = await _dbContext.SerialDatas
                .Where(x => x.ProductId == _vm.Order.ProductId && !x.Used)
                .OrderBy(x => x.SerialNumber)
                .Take(_vm.Order.Quantity)
                .ToListAsync();
            if (_vm.Order.Status != Status.CREATED)
            {
                CstMessageBox.Show(_vm.Order.Number, "WO must be in `CREATED` status!", CstMessageBoxIcon.Error);
            }
            else if (serialList.Count < _vm.Order.Quantity)
            {
                CstMessageBox.Show("Insufficient quantity", $"Total serial number ({serialList.Count}) are less than required quantity ({_vm.Order.Quantity})", CstMessageBoxIcon.Warning);
            }
            else
            {
                string min = serialList.Min(x => x.SerialNumber);
                string max = serialList.Max(x => x.SerialNumber);
                if (CstMessageBox.Confirm($"Start {_vm.Order.Number} ({_vm.Order.Product.Name})",
                    $"Generate serial number range {min}-{max}. Continue?"))
                {
                    rbiLoader.BusyContent = "Generating... Please do not shut down application!";
                    rbiLoader.IsBusy = true;
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += GenerateNumber_DoWork;
                    worker.RunWorkerCompleted += (o, ev) =>
                    {
                        rbiLoader.IsBusy = false;
                        var result = ev.Result as WorkerResultInfo;
                        if (result.Success)
                        {
                            CstMessageBox.Show("Success", result.Message, CstMessageBoxIcon.Success);
                        }
                        else
                        {
                            CstMessageBox.Show("Error", result.Message, CstMessageBoxIcon.Error);
                        }
                        LoadOrders();
                    };
                    worker.RunWorkerAsync(serialList);
                }
            }
        }

        private void GenerateNumber_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerResultInfo result = new WorkerResultInfo();
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    bool productHasInitialize = _vm.Order.Product.HasInitialize.Value;

                    var serialList = e.Argument as List<SerialData>;
                    List<ProductRouter> routers = _dbContext.ProductRouters
                        .Where(x => x.ProductId == _vm.Order.ProductId)
                        .OrderBy(x => x.StationIndex)
                        .ToList();

                    if (routers.Count == 0)
                    {
                        CstMessageBox.Show("Not ready", $"{_dbContext.Products.FirstOrDefault(p => p.Id == _vm.Order.ProductId).Name} dose not exist on ProductRouter table!", CstMessageBoxIcon.Warning);
                    }
                    else
                    {
                        foreach (SerialData serial in serialList)
                        {
                            foreach (ProductRouter router in routers)
                            {
                                // Pass tram dau neu setup san pham khong co tram nay
                                if (!productHasInitialize && router.StationIndex == Station.INITIALIZE)
                                {
                                    _dbContext.SerialRoutings.Add(new SerialRouting()
                                    {
                                        Id = Guid.NewGuid(),
                                        SerialNumber = serial.SerialNumber,
                                        OrderId = _vm.Order.Id,
                                        StationIndex = router.StationIndex,
                                        StationName = router.StationName,
                                        Status = Status.PASSED,
                                        CreatedBy = UserSession.UserID,
                                        CreatedAt = DateTime.Now
                                    });
                                }
                                else
                                {
                                    _dbContext.SerialRoutings.Add(new SerialRouting()
                                    {
                                        Id = Guid.NewGuid(),
                                        SerialNumber = serial.SerialNumber,
                                        OrderId = _vm.Order.Id,
                                        StationIndex = router.StationIndex,
                                        StationName = router.StationName,
                                        Status = Status.CREATED,
                                        CreatedBy = UserSession.UserID,
                                        CreatedAt = DateTime.Now
                                    });
                                }
                            }
                            serial.Used = true;
                        }
                        _vm.Order.Status = Status.STARTED;
                        _vm.Order.StartedAt = DateTime.Now;

                        _dbContext.SaveChanges();
                        transaction.Commit();
                        result.Success = true;
                        result.Message = $"{_vm.Order.Number} started!";
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    result.Message = ex.Message;
                }

                e.Result = result;
            }
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            var pendingCount = _dbContext.SerialRoutings
                .Count(x => x.OrderId == _vm.Order.Id && x.Status == Status.CREATED);

            if (pendingCount > 0)
            {
                CstMessageBox.Show("Not Ready", $"{_vm.Order.Number} has {pendingCount} serial number not running!", CstMessageBoxIcon.Warning);
            }
            else
            {
                _vm.Order.Status = Status.ENDED;
                _vm.Order.EndedAt = DateTime.Now;
            }
        }

        private void RadToggleSwitchButton_Checked(object sender, RoutedEventArgs e)
        {
            _vm.Order = new ProductionOrder();
            _vm.Order.Status = Status.CREATED;
        }

        private void gridLog_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {
            _vm.IsNew = false;
        }
    }
}
