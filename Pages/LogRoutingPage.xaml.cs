using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using Onset_Serialization.Models;
using Onset_Serialization.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for LogRoutingPage.xaml
    /// </summary>
    public partial class LogRoutingPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private AuthUtils _auth = null;
        private ObservableCollection<PrintLogInfo> _dataList = new ObservableCollection<PrintLogInfo>();

        public LogRoutingPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _auth = new AuthUtils(_dbContext.Authorizations.ToList());

            gridLog.ItemsSource = _dataList;
            cbbWO.ItemsSource = await _dbContext.ProductionOrders
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            cbbWO.DisplayMemberPath = "Number";
        }


        private async void cbbWO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var wo = cbbWO.SelectedItem as ProductionOrder;
            if (wo != null)
            {
                var records = await(from r in _dbContext.SerialRoutings
                                    join p in _dbContext.PrintLogs
                                    on r.Id equals p.RefId into prt_rt
                                    from prl in prt_rt.DefaultIfEmpty()
                                    where r.OrderId == wo.Id
                                    orderby r.SerialNumber, r.StationIndex
                                    select new PrintLogInfo()
                                    {
                                        PrintId = prl.Id,
                                        RefId = r.Id,
                                        SerialNumber = r.SerialNumber,
                                        StationIndex = r.StationIndex,
                                        StationName = r.StationName,
                                        Status = r.Status,
                                        PrintedLabel = prl.LabelName,
                                        PrintedData = prl.PrintedData,
                                        CreatedAt = r.CreatedAt,
                                        LastModified = r.ModifiedAt
                                    }).ToListAsync();
                _dataList.Clear();
                foreach (var record in records)
                {
                    _dataList.Add(record);
                }
            }
        }

        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            var log = gridLog.SelectedItem as PrintLogInfo;
            if (log != null)
            {
                if (String.IsNullOrEmpty(log.PrintedData))
                {
                    CstMessageBox.Show("No Data", "Không có dữ liệu in", CstMessageBoxIcon.Warning);
                }
                else
                {
                    string authID;
                    if (_auth.Authenticate(AuthUtils.ACTION_REPRINT, out authID))
                    {
                        try
                        {
                            LabelInfo labelInfo = JsonUtils.Deserialize<LabelInfo>(log.PrintedData);
                            _dbContext.Histories.Add(new History()
                            {
                                Reference = $"{log.RefId}({log.PrintId})",
                                Action = "Reprint",
                                Description = log.PrintedData,
                                CreatedBy = authID,
                                CreatedAt = DateTime.Now,
                                Machine = Environment.MachineName
                            });
                            await _dbContext.SaveChangesAsync();
                            PrintUtils.PrintLabel(log.PrintedLabel, labelInfo);
                        }
                        catch (Exception ex)
                        {
                            CstMessageBox.Show("Error", ex.Message, CstMessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private async void btnReset_Click(object sender, RoutedEventArgs e)
        {
            var log = gridLog.SelectedItem as PrintLogInfo;
            if (log != null)
            {
                if (log.Status == Status.CREATED || log.StationIndex == 1)
                {
                    CstMessageBox.Show("Invalid Data", "Dữ liệu đã ở trạng thái khởi tạo!", CstMessageBoxIcon.Error);
                    return;
                }

                string authID;
                if (_auth.Authenticate(AuthUtils.ACTION_RESET_ROUTE, out authID))
                {
                    using (var transaction = _dbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            SerialRouting current = await _dbContext.SerialRoutings
                                .FirstOrDefaultAsync(x => x.Id == log.RefId);
                            PackageData packData = await _dbContext.PackageDatas
                                .FirstOrDefaultAsync(x => x.SerialNumber == current.SerialNumber);
                            if (await _dbContext.SerialRoutings.AnyAsync(x =>
                                x.SerialNumber == current.SerialNumber &&
                                x.StationIndex > current.StationIndex &&
                                x.Status != Status.CREATED))
                            {
                                CstMessageBox.Show("Reset Denied!", $"`{current.SerialNumber}` đã qua công đoạn sau. Không thể reset dữ liệu này!", CstMessageBoxIcon.Error);
                            }
                            else if (packData != null)
                            {
                                CstMessageBox.Show("Reset Denied!", $"`{current.SerialNumber}` đã được đóng gói vào thùng `{packData.Package.Number}`!", CstMessageBoxIcon.Error);
                            }
                            else
                            {
                                if (CstMessageBox.Confirm($"Reset {current.SerialNumber}",
                                    $"Reset dữ liệu tại trạm {current.StationName}. Tiếp tục?"))
                                {
                                    _dbContext.Histories.Add(new History()
                                    {
                                        Reference = $"{log.RefId}",
                                        Action = "Reset",
                                        Description = JsonUtils.Serialize(log),
                                        CreatedBy = authID,
                                        CreatedAt = DateTime.Now,
                                        Machine = Environment.MachineName
                                    });
                                    _dbContext.PrintLogs.RemoveRange(
                                        _dbContext.PrintLogs.Where(x => x.RefId == current.Id)
                                    );
                                    current.Status = Status.CREATED;
                                    current.ModifiedBy = null;
                                    current.ModifiedAt = null;

                                    await _dbContext.SaveChangesAsync();
                                    transaction.Commit();

                                    // Update UI data
                                    log.Status = current.Status;
                                    log.LastModified = null;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            CstMessageBox.Show("Error", ex.Message, CstMessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}
