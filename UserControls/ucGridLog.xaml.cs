using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Onset_Serialization.UserControls
{
    /// <summary>
    /// Interaction logic for ucGridLog.xaml
    /// </summary>
    public partial class ucGridLog : UserControl
    {
        #region Private variable
        private GridLogActionEventHandler _printEvent;
        private GridLogActionEventHandler _deleteEvent;
        #endregion

        #region Register DependencyProperty (used in XAML)
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns), typeof(Telerik.Windows.Controls.GridViewColumnCollection), typeof(ucGridLog));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(ucGridLog),
            new FrameworkPropertyMetadata(OnItemsSourceChanged) { BindsTwoWayByDefault = true });

        public static readonly DependencyProperty ShowDeleteButtonProperty = DependencyProperty.Register(
            nameof(ShowDeleteButton), typeof(Boolean), typeof(ucGridLog), new PropertyMetadata(false));

        public static readonly DependencyProperty ShowPrintButtonProperty = DependencyProperty.Register(
            nameof(ShowPrintButton), typeof(Boolean), typeof(ucGridLog), new PropertyMetadata(false));

        public static readonly DependencyProperty SearchBoxPlaceholderProperty = DependencyProperty.Register(
            nameof(SearchBoxPlaceholder), typeof(String), typeof(ucGridLog));

        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
            nameof(SelectionMode), typeof(SelectionMode), typeof(ucGridLog), new PropertyMetadata(SelectionMode.Single));
        #endregion

        #region EventHandler
        public delegate void GridLogFilterEventHandler(object sender, GridLogFilterChangedEventArgs e);
        public delegate void GridLogActionEventHandler(object sender, GridLogActionEventArgs e);

        public event GridLogActionEventHandler PrintClicked
        {
            add { _printEvent += value; ShowPrintButton = _printEvent != null; }
            remove { _printEvent -= value; ShowPrintButton = _printEvent != null; }
        }
        public event GridLogActionEventHandler DeleteClicked
        {
            add { _deleteEvent += value; ShowDeleteButton = _deleteEvent != null; }
            remove { _deleteEvent -= value; ShowDeleteButton = _deleteEvent != null; }
        }
        public event GridLogFilterEventHandler FilterChanged;
        #endregion

        #region Properties for DependencyProperty
        public Telerik.Windows.Controls.GridViewColumnCollection Columns
        {
            get => (Telerik.Windows.Controls.GridViewColumnCollection)GetValue(ColumnsProperty);
            set { SetValue(ColumnsProperty, value); }
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public Boolean ShowDeleteButton
        {
            get => (Boolean)GetValue(ShowDeleteButtonProperty);
            set => SetValue(ShowDeleteButtonProperty, value);
        }

        public Boolean ShowPrintButton
        {
            get => (Boolean)GetValue(ShowPrintButtonProperty);
            set => SetValue(ShowPrintButtonProperty, value);
        }

        public String SearchBoxPlaceholder
        {
            get => (String)GetValue(SearchBoxPlaceholderProperty);
            set => SetValue(SearchBoxPlaceholderProperty, value);
        }

        public SelectionMode SelectionMode
        {
            get => (SelectionMode)GetValue(ShowPrintButtonProperty);
            set { SetValue(ShowPrintButtonProperty, value); gridLog.SelectionMode = value; }
        }
        #endregion

        #region User Control Properties
        public String FilterText { get; set; }
        public DateTime? FilterFromDate { get; set; }
        public DateTime? FilterToDate { get; set; }
        #endregion

        #region DependencyPropety Metadata Callback
        protected static void OnItemsSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var uc = sender as ucGridLog;
            uc.gridLog.ItemsSource = e.NewValue;
        }
        #endregion

        public ucGridLog()
        {
            Columns = new Telerik.Windows.Controls.GridViewColumnCollection();
            Columns.CollectionChanged += Columns_CollectionChanged;
            InitializeComponent();
            this.DataContext = this;
        }

        #region UI Event Handler
        private void InvokeFilterHandler()
        {
            var args = new GridLogFilterChangedEventArgs(FilterText, FilterFromDate, FilterToDate);
            FilterChanged?.Invoke(this, args);
        }

        private void Columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            gridLog.Columns.Clear();
            gridLog.Columns.AddRange(Columns);
            gridLog.AutoGenerateColumns = Columns.Count == 0;
            // Defined column filter operator based on data type
            foreach (var column in gridLog.Columns)
            {
                var gridCol = column as Telerik.Windows.Controls.GridViewBoundColumnBase;
                if (gridCol != null)
                {
                    var filterDescriptor = gridCol.ColumnFilterDescriptor.FieldFilter;
                    if (gridCol.DataType == null || gridCol.DataType == typeof(String))
                    {
                        filterDescriptor.Filter1.Operator = Telerik.Windows.Data.FilterOperator.Contains;
                    }
                    else if (gridCol.DataType == typeof(DateTime))
                    {
                        filterDescriptor.Filter1.Operator = Telerik.Windows.Data.FilterOperator.IsGreaterThanOrEqualTo;
                        filterDescriptor.Filter2.Operator = Telerik.Windows.Data.FilterOperator.IsLessThanOrEqualTo;
                    }
                }
            }
        }

        private void RadWatermarkTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                InvokeFilterHandler();
            }
        }

        private void DatePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvokeFilterHandler();
        }

        private void Print_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GridLogActionEventArgs args = new GridLogActionEventArgs(
                gridLog.SelectedItem,
                gridLog.SelectedItems);
            _printEvent?.Invoke(this, args);
        }

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GridLogActionEventArgs args = new GridLogActionEventArgs(
                gridLog.SelectedItem,
                gridLog.SelectedItems);
            _deleteEvent?.Invoke(this, args);
        }
        #endregion
    }

    public class GridLogFilterChangedEventArgs : RoutedEventArgs
    {
        public String Text { get; private set; }
        public DateTime? FromDate { get; private set; }
        public DateTime? ToDate { get; private set; }

        public GridLogFilterChangedEventArgs(String text = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            Text = text;
            FromDate = fromDate;
            ToDate = toDate;
        }
    }

    public class GridLogActionEventArgs : RoutedEventArgs
    {
        public Object SelectedItem { get; private set; }
        public IList<Object> SelectedItems { get; private set; }

        public GridLogActionEventArgs(Object selectedItem, IList<Object> selectedItems)
        {
            SelectedItem = selectedItem;
            SelectedItems = selectedItems;
        }
    }
}
