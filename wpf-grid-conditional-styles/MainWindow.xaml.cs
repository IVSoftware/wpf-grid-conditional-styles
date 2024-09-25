using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;

namespace wpf_grid_conditional_styles
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FinancialMetric.DynamicValueCollectionChanged += (sender, e) =>
            {
                if (sender is FinancialMetric metric)
                {
                    switch (e.Action)
                    {
                        case DynamicValueChangedAction.Add:
                            if (metric[e.Key] is FormattableObject formattable)
                            {
                                // Add column to grid IF NECESSARY.
                                if (HistoricDataGrid.Columns.Any(_ => string.Equals($"{_.Header}", e.Key, StringComparison.OrdinalIgnoreCase)))
                                {   /* G T K */
                                    // Column Exists
                                }
                                else
                                {
                                    var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                                    textBoxFactory.SetBinding(DataContextProperty, new Binding($"[{e.Key}]"));
                                    textBoxFactory.SetBinding(TextBox.TextProperty, new Binding("Text"));
                                    textBoxFactory.SetBinding(TextBox.ForegroundProperty, new Binding("ForeColor"));
                                    textBoxFactory.SetBinding(TextBox.BackgroundProperty, new Binding("BackColor"));
                                    textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(5, 0, 5, 0));
                                    textBoxFactory.SetValue(TextBox.MinWidthProperty, 75d);
                                    textBoxFactory.SetValue(TextBox.IsReadOnlyProperty, true);
                                    var template = new DataTemplate
                                    {
                                        VisualTree = textBoxFactory,
                                    };

                                    HistoricDataGrid.Columns.Add(new DataGridTemplateColumn
                                    {
                                        Header = e.Key,
                                        CellTemplate = template
                                    });
                                }
                            }
                            else Debug.Fail($"Expecting {nameof(FormattableObject)}");
                            break;
                        case DynamicValueChangedAction.Remove:
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
                // Add line items with initial (non-dynamic) formatting
                DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.Growth)
                {
                    {"2022", "-4.0%" }, // Using implicit string CTOR
                    {"2023", " 1.2%" },
                    {"2024", "11.9%" },
                });
                DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.EBIT));
                DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.ROI));
                DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.Revenue)
                {
                    {"2023", new FormattableObject{Target= 999999.00, ForeColor = Brushes.Blue } },
                });
                DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.StockPrice)
                {
                    {"2023", new FormattableObject{Target= 66.22, ForeColor = Brushes.Maroon } },
                    {"2024", new FormattableObject{Target= 11.8, ForeColor = Brushes.Red } },
                });
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

    class FinancialMetric : INotifyPropertyChanged, IEnumerable<KeyValuePair<string, FormattableObject>>
    {
        public FinancialMetric(Metric metric) => Metric = metric;
        public Metric Metric { get; set; }

        // For collection initializer
        public void Add(string key, FormattableObject value) => this[key] = value;
        public IEnumerator<KeyValuePair<string, FormattableObject>> GetEnumerator() => _columns.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _columns.GetEnumerator();

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
                        DynamicValueCollectionChanged?.Invoke(this, new DynamicValueChangedEventArgs(key));
                    }
                }
                else
                {
                    _columns[key] = value;

                    // This is the DYNAMIC GLUE that allows this specific
                    // FinancialMetric to respond to this specific FormattableObject.
                    value.PropertyRequestedFromParent -= ProvidePropertyValue;
                    value.PropertyRequestedFromParent += ProvidePropertyValue;

                    // This is an event, declared static, that informs the grid that
                    // this value goes in a column named {key} which needs to be created
                    // if it doesn't already exist.
                    DynamicValueCollectionChanged?.Invoke(this, new DynamicValueChangedEventArgs(key, value));
                }
                OnPropertyChanged(key);
            }
        }
        private readonly Dictionary<string, FormattableObject> _columns = new();

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ProvidePropertyValue(object? sender, PropertyChangedEventArgs e)
        {
            dynamic generic = e;
            if (sender is FormattableObject formattable)
            {
                var formatTarget = formattable.Target as IFormattable;
                switch (e.PropertyName)
                {
                    case nameof(FormattableObject.Text):
                        // T E X T    S A M P L E S
                        switch (Metric)
                        {
                            case Metric.Revenue:
                                if (formatTarget is null)
                                {
                                    generic.NewValue = formattable.Target?.ToString();
                                }
                                else
                                {
                                    generic.NewValue = formatTarget.ToString("C0", CultureInfo.CurrentCulture);
                                }
                                break;
                            case Metric.StockPrice:
                                if (formatTarget is null)
                                {
                                    generic.NewValue = formattable.Target?.ToString();
                                }
                                else
                                {
                                    generic.NewValue = formatTarget.ToString("C2", CultureInfo.CurrentCulture);
                                }
                                break;
                        }
                        break;
                    case nameof(FormattableObject.ForeColor):
                        // C O L O R    S A M P L E S
                        switch (Metric)
                        {
                            case Metric.Growth:
                                generic.NewValue = $"{formattable.Target}".Contains("-") ?
                                    Brushes.Red : Brushes.Green;
                                break;
                        }
                        break;
                    case nameof(FormattableObject.BackColor):
                        break;
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public static event EventHandler<DynamicValueChangedEventArgs>? DynamicValueCollectionChanged;
    }
    enum DynamicValueChangedAction { Add, Remove }
    class DynamicValueChangedEventArgs : EventArgs
    {
        public DynamicValueChangedEventArgs(string key, object? value = null)
        {
            Key = key;
            Value = value;
            Action = value is null ? DynamicValueChangedAction.Remove : DynamicValueChangedAction.Add;
        }
        public string Key { get; }
        public object? Value { get; }
        public DynamicValueChangedAction Action { get; }
    }
    class FormattableObject : INotifyPropertyChanged
    {
        public static implicit operator FormattableObject(double value) =>
            new FormattableObject { Target = value };

        public static implicit operator FormattableObject(string value) =>
            new FormattableObject { Target = value };
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

        private T? RequestFromParent<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var e = new RequestFromParentEventArgs<T>(propertyName ?? string.Empty);
            PropertyRequestedFromParent?.Invoke(this, e);
            return e.NewValue ?? value;
        }
        public string? Text
        {
            get => RequestFromParent(Target?.ToString());
            set
            {
                // N O O P
                throw new NotImplementedException("TODO: So far, we have only used the TextBox as Read Only.");
            }
        }

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
        public event PropertyChangedEventHandler? PropertyRequestedFromParent;
    }
    public class RequestFromParentEventArgs<T> : PropertyChangedEventArgs
    {
        public RequestFromParentEventArgs(string propertyName)
            : base(propertyName) { }

        public T? NewValue { get; set; }
    }
}