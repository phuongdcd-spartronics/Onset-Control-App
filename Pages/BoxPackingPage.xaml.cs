using Onset_Serialization.Data;
using Onset_Serialization.Enums;
using Onset_Serialization.Labels;
using Onset_Serialization.Models;
using Onset_Serialization.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Onset_Serialization.Pages
{
    /// <summary>
    /// Interaction logic for BoxPackingPage.xaml
    /// </summary>
    public partial class BoxPackingPage : Page
    {
        private OnsetControlEntities _dbContext = new OnsetControlEntities();
        private ObservableCollection<ScanDataInfo> _scannedData = new ObservableCollection<ScanDataInfo>();
        private string _packID = String.Empty;
        private string _packageNumber = String.Empty;
        private Guid _packageID = Guid.Empty;
        private int _boxQuantity = Properties.Settings.Default.Box_Quantity;

        public BoxPackingPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cbbAssembly.ItemsSource = _dbContext.Products
                .Where(x => !x.Obsoleted)
                .ToList();
            cbbAssembly.DisplayMemberPath = "Name";
            lvSerials.ItemsSource = _scannedData;
            pkName.Text = _packageNumber;
        }

        private void ClearUI()
        {
            txtScan.Text = null;
            tbMessage.Text = null;
            txtScan.Focus();
        }

        private async void txtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string data = txtScan.Text;
                tbMessage.Foreground = Brushes.Red;
                ClearUI();

                Product product = cbbAssembly.SelectedItem as Product;
                SerialData serial = await _dbContext.SerialDatas
                    .FirstOrDefaultAsync(x => x.SerialNumber == data);
                SerialRouting route = await _dbContext.SerialRoutings
                    .Where(x => x.SerialNumber == data)
                    .FirstOrDefaultAsync();
                PackageData packData = await _dbContext.PackageDatas
                    .FirstOrDefaultAsync(x => x.SerialNumber == data);

                if (serial == null || route == null)
                {
                    tbMessage.Text = $"`{data}` không tồn tại!";
                    return;
                }
                else if (route.ProductionOrder.ProductId != product.Id)
                {
                    tbMessage.Text = $"`{data}` không thuộc sản phẩm `{product.Name}`";
                    return;
                }
                else if (!String.IsNullOrEmpty(_packID) && serial.Group != _packID)
                {
                    tbMessage.Text = $"`{serial.SerialNumber}`({serial.Group}) không chung nhóm với sản phẩm đang đóng gói ({_packID})!";
                    return;
                }
                else if (_dbContext.SerialRoutings.Any(x => x.SerialNumber == route.SerialNumber && x.Status != Status.PASSED))
                {
                    tbMessage.Text = $"`{route.SerialNumber}` chưa qua hết quy trình!";
                    return;
                }
                else if (packData != null)
                {
                    if (_packageNumber != String.Empty && packData.Package.Number != _packageNumber)
                    {
                        tbMessage.Text = $"`{data}` không được sử dụng đóng gói cho `{_packageNumber}` \n `{data}` được qui định đóng gói cho `{packData.Package.Number}`";
                        return;
                    }

                    bool finished = _dbContext.Packages.Any(p => p.Finished == true && p.Id == packData.PackageId);
                    if (finished)
                    {
                        tbMessage.Text = $"`{data}` đã sử dụng đóng gói cho `{packData.Package.Number}`";
                        return;
                    }
                    else
                    {
                        _packageID = packData.Package.Id;
                        _packageNumber = packData.Package.Number;
                        _boxQuantity = packData.Package.Quantity;
                    }
                }
                if (_scannedData.Any(x => x.Key == route.SerialNumber && x.Status == true))
                {
                    var existObj = _scannedData.First(x => x.Key == route.SerialNumber);
                    tbMessage.Text = $"`{data}` đã được thêm vào";
                    lvSerials.SelectedItem = existObj;
                    lvSerials.ScrollIntoView(existObj);
                    return;
                }

                // Save the first Order ID to be packing
                _packID = serial.Group;
                //_scannedData.Add(new ScanDataInfo()
                //{
                //    Key = route.SerialNumber,
                //    Name = route.SerialNumber,
                //    Time = DateTime.Now
                //});

                if(_packageNumber != String.Empty && _packageID != Guid.Empty && _scannedData.Count == 0)
                {
                    List<PackageData> _lsPackageDate = _dbContext.PackageDatas.Where(p => p.PackageId == _packageID).OrderBy(p => p.SerialNumber).ToList();
                    foreach(var item in _lsPackageDate)
                    {
                        _scannedData.Add(new ScanDataInfo()
                        {
                            Key = item.SerialNumber,
                            Name = item.SerialNumber,
                            Status = false,
                            Time = DateTime.Now
                        });
                    }
                }

                // Set other color for scan data
                var scanObj = _scannedData.First(x => x.Key == data);
                if(scanObj != null)
                {
                    lvSerials.SelectedItem = scanObj;
                    lvSerials.ScrollIntoView(scanObj);
                    scanObj.Status = true;
                }

                // Display serial and quantity of package
                pkName.Text = _packageNumber;
                pkQuantity.Text = _scannedData.Where(s => s.Status == true).ToList().Count.ToString();
                boxQuantity.Text = _boxQuantity.ToString();
                int countData = _scannedData.Where(x => x.Status == true).ToList().Count;

                //if (_scannedData.Count == _boxQuantity)
                if (countData == _boxQuantity)
                {
                    using (var transaction = _dbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            string prefix = _packID;
                            List<Package> pkgs = await _dbContext.Packages.Where(x => x.Prefix == prefix).ToListAsync();
                            int seq = pkgs.Any() ? pkgs.Max(x => x.SeqNo) + 1 : 1;
                            string packageNo = $"{prefix}-{seq.ToString().PadLeft(3, '0')}";
                            Guid packageID = Guid.NewGuid();

                            // Not exist _packNumber
                            if (_packageNumber == String.Empty)
                            {
                                _dbContext.Packages.Add(new Package()
                                {
                                    Id = packageID,
                                    Number = packageNo,
                                    Prefix = prefix,
                                    SeqNo = seq,
                                    Type = "Box Packing",
                                    Quantity = _scannedData.Count,
                                    Materials = "Paperboard",
                                    Finished = true,
                                    Machine = Environment.MachineName,
                                    CreatedBy = UserSession.UserID,
                                    CreatedAt = DateTime.Now
                                });
                                foreach (var pkgData in _scannedData)
                                {
                                    _dbContext.PackageDatas.Add(new PackageData()
                                    {
                                        PackageId = packageID,
                                        SerialNumber = pkgData.Key,
                                        CreatedAt = pkgData.Time
                                    });
                                }
                            }
                            else
                            {
                                packageNo = _packageNumber;
                                packageID = _packageID;
                                Package package = _dbContext.Packages.Where(p => p.Id == _packageID).FirstOrDefault();
                                if(package != null)
                                {
                                    package.Finished = true;
                                    package.Machine = Environment.MachineName;
                                    package.CreatedBy = UserSession.UserID;
                                    _dbContext.SaveChanges();
                                }
                            }

                            LabelInfo info = new LabelInfo()
                            {
                                SerialNumber = packageNo,
                                PO = _packID,
                                Quantity = _boxQuantity.ToString(),
                                ProductName = product.Name
                            };
                            _dbContext.PrintLogs.Add(new PrintLog()
                            {
                                RefId = packageID,
                                SerialNumber = packageNo,
                                LabelName = nameof(Carton_Label),
                                PrintedData = JsonUtils.Serialize(info),
                                Machine = Environment.MachineName,
                                PrintedBy = UserSession.UserID,
                                PrintedDate = DateTime.Now
                            });

                            _dbContext.SaveChanges();
                            transaction.Commit();

                            // Print label
                            PrintLabel(info);

                            // Clear UI;
                            ClearUI();
                            _scannedData.Clear();
                            _packID = String.Empty;
                            tbMessage.Foreground = Brushes.Green;
                            tbMessage.Text = "Successful!";
                            txtLastPackage.Text = packageNo;
                            txtLastSerial.Text = data;
                            pkQuantity.Text = "0";
                            boxQuantity.Text = Properties.Settings.Default.Box_Quantity.ToString();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            tbMessage.Text = ex.Message;
                        }
                    }
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearUI();
        }

        private void lvSerials_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var model = lvSerials.SelectedItem as ScanDataInfo;
            if (model != null)
            {
                if (CstMessageBox.Confirm($"Xóa {model.Key}", "Tiếp tục?"))
                {
                    _scannedData.Remove(model);
                }
            }
        }

        private void PrintLabel(LabelInfo info)
        {
            Carton_Label label = new Carton_Label();
            try 
            {
                label.Print(info);
            }
            catch (Exception ex) { throw ex; }
            finally
            {
                label.Close();
            }
        }
    }
}
