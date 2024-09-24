As I understand it, you're generating some columns dynamically and want to conditionally set some formats based on properties of the line item, even though you only have the one "fixed" column to start. The short answer is to use `DataGridTemplateCOlumn` instead of `DataGridTextColumn`. But before looking at code, consider that it's probably the `FinancialMetric` line item that needs to know what columns that it needs. This way, when a column gets added, the `FinancialMetric` object can fire a static `ColumnChanged` event so that the `HistoricDataGrid` can determine whether it needs to add that column, or if it aleady exists.

The other aspect of this is that when we add a column like year "2023", the associated value should be a class that exposes properties for Text (as in formatted), ForeColor, and BackColor. In the sample below, there's a `FormattedObject` class for this purpose.

With these things in mind, it might make better sense to look at how `HistoricDataGrid` can be populated in a minimal way using the `Loaded` event handler.

```csharp
public MainWindow()
{
    .
    .
    .
    Loaded += (sender, e) =>
    {
        #region T E S T I N G
        // Add line items with initial (non-dynamic) formatting
        DataContext.FinancialMetrics.Add(new FinancialMetric(Metric.Growth)
        {
            {"2022", new FormattableObject{Target= "-4.0%" } },
            {"2023", new FormattableObject{Target= " 1.2%" } },
            {"2024", new FormattableObject{Target= "11.9%" } },
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
```
___


