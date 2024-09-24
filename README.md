As I understand it, you're generating some columns dynamically and want to conditionally set some formats based on properties of the line item, even though you only have the one "fixed" column to start. The short answer is to use `DataGridTemplateCOlumn` instead of `DataGridTextColumn`. But before looking at code, consider that it's probably the `FinancialMetric` line item that needs to know what columns that it needs. This way, when a column gets added, the `FinancialMetric` object can fire a static `ColumnChanged` event so that the `HistoricDataGrid` can determine whether it needs to add that column, or if it aleady exists.

The other aspect of this is that when we add a column like year "2023", the associated value should be a class that exposes properties for Text (as in formatted), ForeColor, and BackColor. In the sample below, there's a `FormattedObject` class for this purpose.

With these things in mind, it might make better sense to look at how `HistoricDataGrid` can be populated in a minimal way using the `Loaded` event handler.
___


