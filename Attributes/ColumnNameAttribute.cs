using System;

namespace MergeTelemetry
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute
    {
        public string ColumnName { get; }
        public ColumnNameAttribute(string columnName) { ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName)); }
    }
}
