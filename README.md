Your specification is for _dynamic_ columns, which makes things more interesting. One way to meet your spec is by using `DataGridTemplateColumn` instead of `DataGridTextColumn`. That's the short answer (the rest is details).

```
public MainWindow()
{
    InitializeComponent();
    .
    .
    .
    // Subscribe to static event
    FinancialMetric.DynamicValueCollectionChanged += (sender, e) =>
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
    .
    .
    .
}
```

___

_It takes a few steps to glue the bindings together, and before I waste your time reading below where I explain things, you might want to [clone](https://github.com/IVSoftware/wpf-grid-conditional-styles.git) and run my working example to verify that this is the kind of behavior you're looking for._
___

#### Example `FinancialMetric` class (represents a line item bound to a grid row)

What the above snippet says about the `FinancialMetric` object that's bound to a given row is that there needs to be an indexer that provides the data context for the individual formatted cell. There's going to be a _key_, the dynamically specified column name, and it needs to return "some object" that can provide the `ForeColor`, `BackColor`, and (formatted) `Text`. 

As a forward reference, we'll be making our own `FormattableObject` class that can wrap any `object` while also providing the cell-specific format info. We know that `FinancialMetric` class must provide an indexer for `FinancialMetric` that can set or get one of these objects, and subscribing to an event that will give `FinancialMetric` the "final say" on the formatting of a given cell value, based on runtime calculations by the containing object. 

```
class FinancialMetric
{
    // The indexer for new Binding($"[{e.Key}]")
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
    .
    .
    .
    public static event EventHandler<DynamicValueChangedEventArgs>? DynamicValueCollectionChanged;
}
```
___

###### USAGE: Create 5 different line items with dynamic column values.

```
// Add line items with initial formatting, but Parent gets the final word.
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
```

[![screenshot][1]][1]
___

#### `FinancialMetric` gets the final say on formatting.

If we have a `FinancialMetric` instance, we could use the expression `var formattedObject = lineItem["2023"]`. This is exactly what the `DataGridTemplateColumn` binding is doing. And since the grid is going to come `get` the value to paint the cell using this `FormattableObject`, the trick is going to be eventing the line item first.

```
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

    private T? RequestFromParent<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        var e = new RequestFromParentEventArgs<T>(propertyName ?? string.Empty);
        PropertyRequestedFromParent?.Invoke(this, e);
        return e.NewValue ?? value;
    }

    // Requesting the formatted value of ForeColor from parent.
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

    // Requesting the formatted value of the Text from parent.
    public string? Text => RequestFromParent(Target?.ToString());

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangedEventHandler? PropertyRequestedFromParent;
    .
    .
    .
    // Implicit conversions
    public static implicit operator FormattableObject(double value) =>
        new FormattableObject { Target = value };

    public static implicit operator FormattableObject(string value) =>
        new FormattableObject { Target = value };
}
```
___

#### Example of Parent handling the formatting request

```
class FinancialMetric : INotifyPropertyChanged, IEnumerable<KeyValuePair<string, FormattableObject>>
{
    .
    .
    .
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
    .
    .
    .
}
```


  [1]: https://i.sstatic.net/TX87w8Jj.png