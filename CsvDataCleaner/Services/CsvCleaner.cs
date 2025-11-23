using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using CsvDataCleaner.Models;

namespace CsvDataCleaner.Services
{
    public class CsvCleaner
    {
        public List<CsvIssue> Analyze(DataTable table, IList<ColumnRule> rules)
        {
            var issues = new List<CsvIssue>();
            if (table.Rows.Count == 0)
                return issues;

            var ruleLookup = new Dictionary<string, ColumnRule>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules)
            {
                if (!string.IsNullOrWhiteSpace(rule.ColumnName) && !ruleLookup.ContainsKey(rule.ColumnName))
                    ruleLookup.Add(rule.ColumnName, rule);
            }

            var seenRows = new HashSet<string>();
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];

                var keyParts = new List<string>();
                foreach (DataColumn col in table.Columns)
                {
                    keyParts.Add(row[col]?.ToString() ?? string.Empty);
                }
                string key = string.Join("||", keyParts);
                if (!string.IsNullOrEmpty(key))
                {
                    if (!seenRows.Add(key))
                    {
                        issues.Add(new CsvIssue
                        {
                            IssueType = "Duplicate",
                            RowIndex = r + 1,
                            ColumnName = string.Empty,
                            Description = "Doppelte Zeile erkannt."
                        });
                    }
                }

                foreach (DataColumn col in table.Columns)
                {
                    string raw = row[col]?.ToString() ?? string.Empty;
                    string value = raw.Trim();
                    bool isEmpty = string.IsNullOrEmpty(value);

                    if (ruleLookup.TryGetValue(col.ColumnName, out var rule))
                    {
                        if (rule.IsRequired && isEmpty)
                        {
                            issues.Add(new CsvIssue
                            {
                                IssueType = "Required",
                                RowIndex = r + 1,
                                ColumnName = col.ColumnName,
                                Description = "Pflichtfeld ist leer."
                            });
                        }

                        if (!isEmpty)
                        {
                            switch (rule.RuleType)
                            {
                                case RuleType.Numeric:
                                    if (!double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                                    {
                                        issues.Add(new CsvIssue
                                        {
                                            IssueType = "Type",
                                            RowIndex = r + 1,
                                            ColumnName = col.ColumnName,
                                            Description = $"Wert '{value}' ist nicht numerisch."
                                        });
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(rule.MinValue) &&
                                            double.TryParse(rule.MinValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double min) &&
                                            num < min)
                                        {
                                            issues.Add(new CsvIssue
                                            {
                                                IssueType = "Range",
                                                RowIndex = r + 1,
                                                ColumnName = col.ColumnName,
                                                Description = $"Wert {num} ist kleiner als Min {min}."
                                            });
                                        }

                                        if (!string.IsNullOrWhiteSpace(rule.MaxValue) &&
                                            double.TryParse(rule.MaxValue.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double max) &&
                                            num > max)
                                        {
                                            issues.Add(new CsvIssue
                                            {
                                                IssueType = "Range",
                                                RowIndex = r + 1,
                                                ColumnName = col.ColumnName,
                                                Description = $"Wert {num} ist größer als Max {max}."
                                            });
                                        }
                                    }
                                    break;

                                case RuleType.Date:
                                    if (!DateTime.TryParse(value, out DateTime dt))
                                    {
                                        issues.Add(new CsvIssue
                                        {
                                            IssueType = "Type",
                                            RowIndex = r + 1,
                                            ColumnName = col.ColumnName,
                                            Description = $"Wert '{value}' ist kein gültiges Datum."
                                        });
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrWhiteSpace(rule.MinValue) &&
                                            DateTime.TryParse(rule.MinValue, out DateTime dtMin) &&
                                            dt < dtMin)
                                        {
                                            issues.Add(new CsvIssue
                                            {
                                                IssueType = "Range",
                                                RowIndex = r + 1,
                                                ColumnName = col.ColumnName,
                                                Description = $"Datum {dt:yyyy-MM-dd} liegt vor Min {dtMin:yyyy-MM-dd}."
                                            });
                                        }

                                        if (!string.IsNullOrWhiteSpace(rule.MaxValue) &&
                                            DateTime.TryParse(rule.MaxValue, out DateTime dtMax) &&
                                            dt > dtMax)
                                        {
                                            issues.Add(new CsvIssue
                                            {
                                                IssueType = "Range",
                                                RowIndex = r + 1,
                                                ColumnName = col.ColumnName,
                                                Description = $"Datum {dt:yyyy-MM-dd} liegt nach Max {dtMax:yyyy-MM-dd}."
                                            });
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return issues;
        }

        public void ApplyAutoFixes(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (row[col] is string s)
                    {
                        var trimmed = s.Trim();
                        if (!ReferenceEquals(s, trimmed))
                            row[col] = trimmed;
                    }
                }
            }
        }

        public double CalculateQualityScore(DataTable table, IList<CsvIssue> issues)
        {
            if (table.Rows.Count == 0)
                return 100.0;

            double baseScore = 100.0;
            double penaltyPerIssue = 100.0 / (table.Rows.Count * table.Columns.Count + 1);
            double score = baseScore - issues.Count * penaltyPerIssue;
            return score < 0 ? 0 : score;
        }
    }
}
