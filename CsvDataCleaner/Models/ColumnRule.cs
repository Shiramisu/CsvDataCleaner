using System;

namespace CsvDataCleaner.Models
{
    public enum RuleType
    {
        Text,
        Numeric,
        Date
    }

    public class ColumnRule
    {
        public string ColumnName { get; set; } = string.Empty;
        public RuleType RuleType { get; set; } = RuleType.Text;
        public bool IsRequired { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }

        public override string ToString() => $"{ColumnName} ({RuleType})";
    }
}
