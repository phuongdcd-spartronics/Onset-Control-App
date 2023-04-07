using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using Onset_Serialization.Utilities;
using Onset_Serialization.ViewModels;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for ComponentReworkPage.xaml
    /// </summary>
    public partial class ComponentReworkPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ComponentReworkViewModel _vm = new ComponentReworkViewModel();

        public ComponentReworkPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = _vm;
        }

        private void btnRework_Click(object sender, RoutedEventArgs e)
        {
            var comp = _dbContext.SerialComponents
                .FirstOrDefault(x => x.SerialNumber == _vm.SerialNumber && x.ComponentNumber == _vm.CurrentComp);
            if (comp == null)
            {
                CstMessageBox.Show("Not Found", $"Not Found Record for `{_vm.SerialNumber}` with `{_vm.CurrentComp}`", CstMessageBoxIcon.Error);
                return;
            }

            // Accepted rework only WO Started
            var routing = _dbContext.SerialRoutings
                .FirstOrDefault(x => x.SerialNumber == comp.SerialNumber && x.StationIndex == comp.StationIndex);
            if (routing.ProductionOrder.Status != Status.STARTED)
            {
                CstMessageBox.Show("Denied", $"Serial Number of `{routing.ProductionOrder.Number}` is not started!", CstMessageBoxIcon.Warning);
                return;
            }

            // Validate for barcode format of new component
            var router = _dbContext.ProductRouters
                .FirstOrDefault(x => x.ProductId == comp.SerialData.ProductId && x.StationIndex == comp.StationIndex);
            if (router.RouterInputs != null)
            {
                foreach (var input in router.RouterInputs)
                {
                    if (Regex.IsMatch(_vm.CurrentComp, input.Pattern))
                    {
                        if (!Regex.IsMatch(_vm.NewComp, input.Pattern))
                        {
                            CstMessageBox.Show("Incorrect Pattern", $"`{_vm.NewComp}` is in incorrect format!", CstMessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }

            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    _dbContext.SerialComponents.Add(new SerialComponent()
                    {
                        SerialNumber = comp.SerialNumber,
                        ComponentNumber = _vm.NewComp,
                        StationIndex = comp.StationIndex,
                        CreatedBy = UserSession.UserID,
                        CreatedAt = DateTime.Now
                    });
                    _dbContext.SerialComponents.Remove(comp);
                    _dbContext.Histories.Add(new History()
                    {
                        Reference = $"{comp.SerialNumber}${comp.ComponentNumber}",
                        Action = "Replace",
                        Description = $"{JsonUtils.Serialize(_vm)}",
                        Machine = Environment.MachineName,
                        CreatedBy = UserSession.UserID,
                        CreatedAt = DateTime.Now
                    });

                    _dbContext.SaveChanges();
                    transaction.Commit();

                    CstMessageBox.Show("Success", $"`{_vm.CurrentComp}` has been replaced!", CstMessageBoxIcon.Success);
                    _vm = new ComponentReworkViewModel();
                    this.DataContext = _vm;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    CstMessageBox.Show("Error", ex.Message, CstMessageBoxIcon.Error);
                }
            }
        }

        private void txtSerial_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtCurrentComp.Focus();
            }
        }

        private void txtCurrentComp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtNewComp.Focus();
            }
        }

        private void txtNewComp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtReason.Focus();
            }
        }
    }
}
