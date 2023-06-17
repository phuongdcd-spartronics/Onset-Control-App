using Microsoft.Win32;
using OfficeOpenXml;
using Onset_Serialization.Data;
using Onset_Serialization.Models;
using Onset_Serialization.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for PackageGeneratorPage.xaml
    /// </summary>
    public partial class PackageGeneratorPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private string filePath = string.Empty;
        private PackageGeneratorViewModel _vm = new PackageGeneratorViewModel();

        public PackageGeneratorPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.Products = _dbContext.Products.Where(x => !x.Obsoleted).ToList();
            this.DataContext = _vm;
        }

        private void btOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel files (*.xls)|*.xlsx";
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                FileInfo fileInfo = new FileInfo(filePath);
                tbMessage.Text = fileInfo.Name;
            }
        }

        private void cbbAssembly_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadPOData();
        }

        private async void LoadPOData()
        {
            _vm.ProductGroup.Clear();
            if (_vm.SelectedProduct != null)
            {
                var _productGroup = await _dbContext.SerialDatas.Where(x => x.ProductId == _vm.SelectedProduct.Id).Select(x => x.Group).Distinct().ToListAsync();
                foreach(var item in _productGroup)
                {
                    _vm.ProductGroup.Add(new ProductGroup { ProductId = _vm.SelectedProduct.Id, PONumber = item });
                }
            }
        }

        private void cbbPO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadPackage();
        }

        private async void LoadPackage()
        {
            _vm.Packages.Clear();
            if (_vm.SelectedProductGroup != null)
            {
                var _packageList = await _dbContext.Packages
                    .Where(x => x.Prefix == _vm.SelectedProductGroup.PONumber)
                    .OrderBy(x=>x.SeqNo)
                    .ToListAsync();
                foreach (var item in _packageList)
                {
                    _vm.Packages.Add(item);
                }
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if(filePath != string.Empty)
            {
                if (CstMessageBox.Confirm($"Generate package data", $"Continue to generate package data with this file?"))
                {
                    rbiLoader.BusyContent = "Generating... Please do not shut down application!";
                    rbiLoader.IsBusy = true;
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += GeneratePackage_DoWork;
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
                        LoadPackage();
                        filePath = string.Empty;
                    };
                    worker.RunWorkerAsync();
                }
                
                //MessageBox.Show("The package number has been generated successfully.", "Information", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);
            }
            else
            {
                MessageBox.Show("Template file doesn't existed. Please try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                return;
            }
        }

        private void GeneratePackage_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerResultInfo result = new WorkerResultInfo();

            // Read excel file
            FileInfo existingFile = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                int sheetCount = package.Workbook.Worksheets.Count;
                for (int idx = 0; idx < sheetCount; idx++)
                {
                    using (var objContext = new OnsetControlEntities())
                    {
                        using (var tran = objContext.Database.BeginTransaction())
                        {
                            try
                            {
                                ExcelWorksheet worksheet = package.Workbook.Worksheets[idx];
                                int colCount = 1; // worksheet.Dimension.End.Column;
                                int rowCount = worksheet.Dimension.End.Row;

                                // Generate package
                                int seq = Convert.ToInt32(worksheet.Name.Trim());
                                Guid _packageID = Guid.NewGuid();
                                string _prefix = _vm.SelectedProductGroup.PONumber;
                                string packageNumber = $"{_prefix}-{seq.ToString().PadLeft(3, '0')}";
                                if (objContext.Packages.Where(x => x.Number == packageNumber).Count() > 0)
                                {
                                    MessageBox.Show("Package number " + packageNumber + " already exists. Please try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.Yes);
                                    return;
                                }
                                objContext.Packages.Add(new Package()
                                {
                                    Id = _packageID,
                                    Number = packageNumber,
                                    Prefix = _prefix,
                                    SeqNo = seq,
                                    Type = "Box Packing",
                                    Quantity = rowCount,
                                    Materials = "Paperboard",
                                    Finished = false,
                                    Machine = Environment.MachineName,
                                    CreatedBy = UserSession.UserID,
                                    CreatedAt = DateTime.Now
                                });
                                objContext.SaveChanges();

                                for (int row = 1; row <= rowCount; row++)
                                {
                                    for (int col = 1; col <= colCount; col++)
                                    {
                                        // Generate package data
                                        objContext.PackageDatas.Add(new PackageData()
                                        {
                                            PackageId = _packageID,
                                            SerialNumber = worksheet.Cells[row, col].Value?.ToString().Trim(),
                                            CreatedAt = DateTime.Now
                                        });
                                    }
                                }
                                objContext.SaveChanges();
                                tran.Commit();
                                result.Success = true;
                                result.Message = $"Package data has been generated successfully!";
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                //throw ex;
                                result.Message = ex.Message;
                            }
                        }
                    }
                }
            }
            
            e.Result = result;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
