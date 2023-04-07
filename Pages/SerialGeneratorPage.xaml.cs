using Onset_Serialization.Data;
using Onset_Serialization.ViewModels;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Data;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for SerialGeneratorPage.xaml
    /// </summary>
    public partial class SerialGeneratorPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private SerialGeneratorViewModel _vm = new SerialGeneratorViewModel();

        public SerialGeneratorPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.Products = _dbContext.Products
                .Where(x => !x.Obsoleted)
                .ToList();

            this.DataContext = _vm;
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedProduct == null)
            {
                CstMessageBox.Show("Invalid Input", "Please select an assembly!", CstMessageBoxIcon.Warning);
            }
            else if (String.IsNullOrWhiteSpace(_vm.PO))
            {
                CstMessageBox.Show("Invalid Input", "PO is required!", CstMessageBoxIcon.Warning);
            }
            else if (_vm.From < 1 || _vm.To < _vm.From)
            {
                CstMessageBox.Show("Invalid Input", "Invalid serial range number!", CstMessageBoxIcon.Error);
            }
            else
            {
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        for (int sr = _vm.From; sr <= _vm.To; sr++)
                        {
                            _dbContext.SerialDatas.Add(new SerialData()
                            {
                                SerialNumber = $"{sr}",
                                ProductId = _vm.SelectedProduct.Id,
                                Group = _vm.PO,
                                SerialType = "Assembly",
                                Used = false,
                                CreatedBy = UserSession.UserID,
                                CreatedAt = DateTime.Now
                            });
                        }

                        _dbContext.SaveChanges();
                        transaction.Commit();
                    }
                    catch (DbUpdateException)
                    {
                        transaction.Rollback();
                        CstMessageBox.Show("Error", "Some serial number already existed!", CstMessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        CstMessageBox.Show("Error", ex.InnerException?.Message ?? ex.Message, CstMessageBoxIcon.Error);
                    }
                }
                LoadSerialData();
            }
        }

        private void cbbAssembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSerialData();
        }

        private void txtPO_LostFocus(object sender, RoutedEventArgs e)
        {
            LoadSerialData();
        }

        private async void LoadSerialData()
        {
            if (_vm.SelectedProduct != null)
            {
                string po = _vm.PO;

                var serialList = await _dbContext.SerialDatas
                    .Where(x => x.ProductId == _vm.SelectedProduct.Id)
                    .ToListAsync();
                int total = serialList.Count;
                int used = serialList.Where(x => x.Used).Count();
                int remain = total - used;

                tbAssembly.Text = $"Assembly: {_vm.SelectedProduct.Name}";
                tbTotal.Text = $"Total: {total}";
                tbUsed.Text = $"Used: {used}";
                tbRemain.Text = $"Remaining: {remain}";
                tbPO.Text = $"PO#:\n";
                foreach (string group in serialList.Select(x => x.Group).Distinct())
                {
                    var groupList = serialList.Where(x => x.Group == group);
                    int groupTotal = groupList.Count();
                    int groupUsed = groupList.Where(x => x.Used).Count();
                    int groupRemain = groupTotal - groupUsed;
                    tbPO.Text += $"{group}: {groupRemain}/{groupTotal} (Used: {groupUsed})\n";
                }
            }
        }
    }
}
