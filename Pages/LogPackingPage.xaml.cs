using Onset_Serialization.Data;
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
    /// Interaction logic for LogPackingPage.xaml
    /// </summary>
    public partial class LogPackingPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private AuthUtils _auth = null;
        private ObservableCollection<PrintLogInfo> _dataList = new ObservableCollection<PrintLogInfo>();

        public LogPackingPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _auth = new AuthUtils(_dbContext.Authorizations.ToList());

            gridLog.ItemsSource = _dataList;
            cbbWO.ItemsSource = await _dbContext.SerialDatas
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Group)
                .Distinct()
                .ToListAsync();
        }


        private async void cbbWO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string po = cbbWO.SelectedItem?.ToString();
            if (po != null)
            {
                var records = await (from pk in _dbContext.Packages
                                     join pl in _dbContext.PrintLogs
                                     on pk.Id equals pl.RefId into prt_rt
                                     from prl in prt_rt.DefaultIfEmpty()
                                     where pk.Prefix == po
                                     orderby pk.CreatedAt
                                     select new PrintLogInfo()
                                     {
                                         PrintId = prl.Id,
                                         RefId = pk.Id,
                                         SerialNumber = pk.Number,
                                         StationIndex = 9,
                                         StationName = "Shipping",
                                         Status = pk.Finished ? "Completed" : "Pending",
                                         PrintedLabel = prl.LabelName,
                                         PrintedData = prl.PrintedData,
                                         CreatedAt = pk.CreatedAt,
                                         LastModified = null
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
    }
}
