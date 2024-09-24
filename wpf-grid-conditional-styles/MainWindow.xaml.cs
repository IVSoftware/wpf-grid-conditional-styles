using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace wpf_grid_conditional_styles
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FinancialMetric.ColumnChanged += (sender, e) =>
            {
                if (sender is FinancialMetric metric)
                {
                    if (HistoricDataGrid.Columns.Any(_ => string.Equals($"{_.Header}", e.Key, StringComparison.OrdinalIgnoreCase)))
                    {   /* G T K */
                        // Column Exists
                    }
                    else
                    {
                        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"[{e.Key}].FormattedText"));
                        textBlockFactory.SetBinding(TextBlock.ForegroundProperty, new Binding($"[{e.Key}].ForeColor"));
                        textBlockFactory.SetBinding(TextBlock.BackgroundProperty, new Binding($"[{e.Key}].BackColor"));

                        var template = new DataTemplate
                        {
                            VisualTree = textBlockFactory,
                        };

                        HistoricDataGrid.Columns.Add(new DataGridTemplateColumn
                        {
                            Header = e.Key,
                            CellTemplate = template,
                        });
                    }
                };
            };
            Loaded += (sender, e) =>
            {
                // Initialize Test Data
                foreach (var metric in Enum.GetValues<Metric>())
                {
                    var lineItem = new FinancialMetric
                    {
                        Metric = metric,
                    };
                    switch (metric)
                    {
                        case Metric.StockPrice:
                            lineItem["2023"] = new FormattableObject
                            {
                                Target = 66.22,
                                ForeColor = Brushes.Red,
                            };
                            lineItem["2024"] = new FormattableObject
                            {
                                Target = 11.80,
                                ForeColor = Brushes.Green,
                            }; 
                            break;
                        default:
                            break;
                    }
                    DataContext.FinancialMetrics.Add(lineItem);
                }
            };
        }
        new MainWindowViewModel DataContext => (MainWindowViewModel)base.DataContext;
    }
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DataGridColumn> Columns { get; } =
            new ObservableCollection<DataGridColumn>();

        public ObservableCollection<FinancialMetric> FinancialMetrics { get; }
            = new ObservableCollection<FinancialMetric>();
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    public enum Metric
    {
        Revenue,
        NetIncome,
        EBIT,
        GrossMargin,
        ROI,
        StockPrice,
        Growth,
    }

    class FinancialMetric : INotifyPropertyChanged
    {
        public Metric Metric
        {
            get => _metric;
            set
            {
                if (!Equals(_metric, value))
                {
                    _metric = value;
                    OnPropertyChanged();
                }
            }
        }
        Metric _metric = default; private object? _target;

        public FormattableObject? this[string key]
        {
            get => _columns.TryGetValue(key, out var value) ? value : null;
            set
            {
                if (value is null)
                {
                    if (_columns.ContainsKey(key))
                    {
                        _columns.Remove(key);
                        ColumnChanged?.Invoke(this, new ColumnChangeEventArgs(key));
                    }
                }
                else
                {
                    _columns[key] = value;
                    ColumnChanged?.Invoke(this, new ColumnChangeEventArgs(key, value));
                }
                OnPropertyChanged(key);
            }
        }
        private readonly Dictionary<string, FormattableObject> _columns = new();

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
        public static event EventHandler<ColumnChangeEventArgs>? ColumnChanged;
    }
    enum ColumnChangeAction { Add, Remove }
    class ColumnChangeEventArgs : EventArgs
    {
        public ColumnChangeEventArgs(string key, object? value = null)
        {
            Key = key;
            Value = value;
            Action = value is null ? ColumnChangeAction.Remove : ColumnChangeAction.Add;
        }
        public string Key { get; }
        public object? Value { get; }
        ColumnChangeAction Action { get; }
    }
    class FormattableObject : INotifyPropertyChanged
    {
        public object? Target { get; set; }
        public FinancialMetric? Parent { get; set; }
        public virtual string? FormattedText => Target?.ToString();
        public Brush? ForeColor { get; set; } = Brushes.Black;
        public Brush? BackColor { get; set; } = Brushes.White;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}