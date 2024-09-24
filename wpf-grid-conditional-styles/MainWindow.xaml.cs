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
                        HistoricDataGrid.Columns.Add(new DataGridTemplateColumn
                        {
                            Header = e.Key,
                            Binding = new Binding($"[{e.Key}]")
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
                            lineItem["2023"] = 66.22;
                            lineItem["2024"] = 11.80;
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
#if false
        public virtual string DisplayValue
        {
            get
            {
                // Opportunity to customize.
                switch (Metric)
                {
                    case Metric.Revenue:
                    case Metric.NetIncome:
                    case Metric.EBIT:
                    case Metric.GrossMargin:
                    case Metric.ROI:
                    case Metric.StockPrice:
                    default:
                        return $"{Metric}";
                }
            }
        }
        string _displayValue = string.Empty;
#endif


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
        Metric _metric = default;

        public object? this[string key]
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
        private readonly Dictionary<string, object> _columns = new();

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
    public class FormattableObject : INotifyPropertyChanged
    {
        private object? _target;
        public object? Target
        {
            get => _target;
            set
            {
                if (!Equals(_target, value))
                {
                    _target = value;
                    OnPropertyChanged();
                    UpdateFormattedText();
                    UpdateColors();
                }
            }
        }

        private Brush? _foreColor;
        public Brush? ForeColor
        {
            get => _foreColor;
            set
            {
                if (!Equals(_foreColor, value))
                {
                    _foreColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Brush? _backColor;
        public Brush? BackColor
        {
            get => _backColor;
            set
            {
                if (!Equals(_backColor, value))
                {
                    _backColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _formattedText;
        public string? FormattedText
        {
            get => _formattedText;
            private set
            {
                if (_formattedText != value)
                {
                    _formattedText = value;
                    OnPropertyChanged();
                }
            }
        }

        public FormattableObject(object? target = null)
        {
            Target = target;
        }

        private void UpdateFormattedText()
        {
            // Implement custom formatting logic here
            if (Target is double numericValue)
            {
                FormattedText = numericValue.ToString("N2");
            }
            else if (Target != null)
            {
                FormattedText = Target.ToString();
            }
            else
            {
                FormattedText = string.Empty;
            }
        }

        private void UpdateColors()
        {
            // Implement custom color logic here
            if (Target is double numericValue)
            {
                ForeColor = numericValue >= 0 ? Brushes.Green : Brushes.Red;
                BackColor = Brushes.Transparent;
            }
            else
            {
                ForeColor = Brushes.Black;
                BackColor = Brushes.Transparent;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}