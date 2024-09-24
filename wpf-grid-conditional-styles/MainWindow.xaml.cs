using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Diagnostics;
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
                    switch (e.Action)
                    {
                        case ColumnChangeAction.Add:
                            // Add column to grid IF NECESSARY.
                            if (HistoricDataGrid.Columns.Any(_ => string.Equals($"{_.Header}", e.Key, StringComparison.OrdinalIgnoreCase)))
                            {   /* G T K */
                                // Column Exists
                            }
                            else
                            {
                                if (metric[e.Key] is FormattableObject formattable)
                                {
                                    var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                                    textBlockFactory.SetBinding(DataContextProperty, new Binding($"[{e.Key}]"));
                                    textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Text"));
                                    textBlockFactory.SetBinding(TextBlock.ForegroundProperty, new Binding("ForeColor"));
                                    textBlockFactory.SetBinding(TextBlock.BackgroundProperty, new Binding("BackColor"));
                                    textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(5, 0, 5, 0));

                                    var template = new DataTemplate
                                    {
                                        VisualTree = textBlockFactory,
                                    };

                                    HistoricDataGrid.Columns.Add(new DataGridTemplateColumn
                                    {
                                        Header = e.Key,
                                        CellTemplate = template
                                    });
                                }
                                else Debug.Fail($"Expecting {nameof(FormattableObject)}");
                            }
                            // Refresh listener always.
                            
                            break;
                        case ColumnChangeAction.Remove:
                            // Check to see whether the key is still in use before removing.
                            if (!DataContext.FinancialMetrics
                                .Any(fm => fm != metric && fm[e.Key] != null))
                            {
                                if (
                                    HistoricDataGrid
                                    .Columns
                                    .FirstOrDefault(_ => string.Equals($"{_.Header}", e.Key, StringComparison.OrdinalIgnoreCase))
                                    is DataGridColumn remove)
                                {
                                    HistoricDataGrid.Columns.Remove(remove);
                                }
                            }
                            break;
                    }
                };
            };
            Loaded += (sender, e) =>
            {
                #region T E S T I N G
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
                #endregion T E S T I N G
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
        public ColumnChangeAction Action { get; }
    }
    class FormattableObject : INotifyPropertyChanged
    {
        public object? Target
        {
            get => _target;
            set
            {
                if (!Equals(_target, value))
                {
                    _target = value;
                    OnPropertyChanged();
                }
            }
        }
        object? _target = default;

        private T? RequestFromParent<T>(T value)
        {
            var e = new RequestFromParentEventArgs<T>();
            PropertyRequestedFromParent?.Invoke(this, e);
            return e.NewValue ?? value;
        }
        public string? Text => RequestFromParent(Target?.ToString());

        public Brush? ForeColor
        {
            get => RequestFromParent(_foreColor);
            set
            {
                if (_foreColor != value)
                {
                    _foreColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private Brush? _foreColor = Brushes.Black;

        public Brush? BackColor
        {
            get => RequestFromParent(_backColor);
            set
            {
                if (_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged();
                }
            }
        }
        private Brush? _backColor = Brushes.White;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
        public event PropertyChangedEventHandler?  PropertyRequestedFromParent;
    }
    public class RequestFromParentEventArgs<T> : PropertyChangedEventArgs
    {
        public RequestFromParentEventArgs([CallerMemberName] string? propertyName = null)
            : base(propertyName) { }

        public T? NewValue { get; set; }
    }
}