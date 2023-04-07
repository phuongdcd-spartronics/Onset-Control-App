using Onset_Serialization.Data;
using Onset_Serialization.Models;
using Onset_Serialization.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Controls;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for LogComponentPage.xaml
    /// </summary>
    public partial class LogComponentPage : Page
    {
        private readonly OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ObservableCollection<ComponentInfo> _logs = new ObservableCollection<ComponentInfo>();

        public LogComponentPage()
        {
            InitializeComponent();

            DateTime today = DateTime.Now.Date;
            ucLog.FilterFromDate = today.AddMonths(-1);
            ucLog.FilterToDate = today;
            ucLog.ItemsSource = _logs;
        }

        private void ucLog_FilterChanged(object sender, UserControls.GridLogFilterChangedEventArgs e)
        {
            DateTime? from = e.FromDate;
            DateTime? to = e.ToDate;

            if (from.HasValue && to.HasValue)
            {
                // Search to 23h59m of to date
                to = to.Value.AddDays(1);
                var data = _dbContext.SerialComponents
                    .Where(x => x.CreatedAt >= from.Value && x.CreatedAt <= to.Value)
                    .Select(x => new ComponentInfo()
                    {
                        SerialNumber = x.SerialNumber,
                        ComponentNumber = x.ComponentNumber,
                        ProductNumber = x.SerialData.Product.Name,
                        CreatedDate = x.CreatedAt
                    })
                    .ToList();

                _logs.Clear();
                foreach (var log in data)
                {
                    _logs.Add(log);
                }
            }
        }
    }
}
