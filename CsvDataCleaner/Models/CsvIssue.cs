namespace CsvDataCleaner.Models
{
    public class CsvIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
