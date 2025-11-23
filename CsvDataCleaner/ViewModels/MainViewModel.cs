using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CsvDataCleaner.Infrastructure;
using CsvDataCleaner.Models;
using CsvDataCleaner.Services;
using Microsoft.Win32;

namespace CsvDataCleaner.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly CsvLoader _loader;
        private readonly CsvCleaner _cleaner;

        private DataView? _csvView;
        private string? _filePath;
        private string _statusText = "Bereit.";
        private bool _isDarkMode;
        private double _qualityScore;
        private int _rowCount;

        public MainViewModel()
        {
            _loader = new CsvLoader();
            _cleaner = new CsvCleaner();

            Issues = new ObservableCollection<CsvIssue>();
            Rules = new ObservableCollection<ColumnRule>();

            OpenFileCommand = new RelayCommand(_ => OpenFile());
            AnalyzeCommand = new RelayCommand(_ => Analyze(), _ => HasData);
            ApplyFixesCommand = new RelayCommand(_ => ApplyFixes(), _ => CanApplyFixes);
            ExportCleanedCommand = new RelayCommand(_ => ExportCleaned(), _ => HasData);
            ExportIssuesCommand = new RelayCommand(_ => ExportIssues(), _ => HasIssues);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DataView? CsvView
        {
            get => _csvView;
            set
            {
                _csvView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasData));
            }
        }

        public ObservableCollection<CsvIssue> Issues { get; }

        public ObservableCollection<ColumnRule> Rules { get; }

        public string? FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    ApplyTheme();
                }
            }
        }

        public double QualityScore
        {
            get => _qualityScore;
            set { _qualityScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(QualityDisplay)); }
        }

        public string QualityDisplay => $"{QualityScore:0.0} %";

        public int RowCount
        {
            get => _rowCount;
            set { _rowCount = value; OnPropertyChanged(); }
        }

        public int IssueCount => Issues.Count;

        public bool HasData => CsvView != null && CsvView.Table.Rows.Count > 0;
        public bool HasIssues => Issues.Count > 0;
        public bool CanApplyFixes => HasData;

        public ICommand OpenFileCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand ApplyFixesCommand { get; }
        public ICommand ExportCleanedCommand { get; }
        public ICommand ExportIssuesCommand { get; }

        private void OpenFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "CSV-Dateien (*.csv;*.txt)|*.csv;*.txt|Alle Dateien (*.*)|*.*",
                Title = "CSV-Datei auswählen"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var table = _loader.LoadCsv(dlg.FileName);
                    CsvView = table.DefaultView;
                    FilePath = dlg.FileName;
                    RowCount = table.Rows.Count;

                    BuildDefaultRules(table);

                    Issues.Clear();
                    QualityScore = 100;
                    StatusText = $"Datei geladen: {Path.GetFileName(dlg.FileName)}";
                    OnPropertyChanged(nameof(IssueCount));
                    OnPropertyChanged(nameof(HasIssues));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden der Datei:\n{ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText = "Fehler beim Laden der Datei.";
                }
            }
        }

        private void BuildDefaultRules(DataTable table)
        {
            Rules.Clear();
            foreach (DataColumn col in table.Columns)
            {
                Rules.Add(new ColumnRule
                {
                    ColumnName = col.ColumnName,
                    RuleType = RuleType.Text,
                    IsRequired = false
                });
            }
        }

        private void Analyze()
        {
            if (CsvView == null)
                return;

            DataTable table = CsvView.Table;
            Issues.Clear();

            var issues = _cleaner.Analyze(table, Rules);
            foreach (var issue in issues)
                Issues.Add(issue);

            QualityScore = _cleaner.CalculateQualityScore(table, issues);
            StatusText = $"Analyse abgeschlossen. Issues: {Issues.Count}, Qualität: {QualityScore:0.0}%";

            OnPropertyChanged(nameof(IssueCount));
            OnPropertyChanged(nameof(HasIssues));
        }

        private void ApplyFixes()
        {
            if (CsvView == null)
                return;

            var table = CsvView.Table;
            _cleaner.ApplyAutoFixes(table);

            Analyze();
            StatusText = "Auto-Fixes angewendet und neu analysiert.";
        }

        private void ExportCleaned()
        {
            if (CsvView == null)
                return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                FileName = "Cleaned.csv",
                Title = "Bereinigte CSV exportieren"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var table = CsvView.Table;
                    var sb = new StringBuilder();

                    // Header
                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        if (c > 0) sb.Append(';');
                        sb.Append(Escape(table.Columns[c].ColumnName));
                    }
                    sb.AppendLine();

                    // Rows
                    foreach (DataRow row in table.Rows)
                    {
                        for (int c = 0; c < table.Columns.Count; c++)
                        {
                            if (c > 0) sb.Append(';');
                            string value = row[c]?.ToString() ?? string.Empty;
                            sb.Append(Escape(value));
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
                    StatusText = $"Bereinigte CSV exportiert: {dlg.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Export:\n{ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText = "Fehler beim Export.";
                }
            }
        }

        private void ExportIssues()
        {
            if (Issues.Count == 0)
                return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                FileName = "Issues.csv",
                Title = "Issues exportieren"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("IssueType;RowIndex;ColumnName;Description");

                    foreach (var issue in Issues)
                    {
                        sb.AppendLine(string.Join(";", new[]
                        {
                            Escape(issue.IssueType),
                            issue.RowIndex.ToString(),
                            Escape(issue.ColumnName),
                            Escape(issue.Description)
                        }));
                    }

                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
                    StatusText = $"Issues exportiert: {dlg.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Export:\n{ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText = "Fehler beim Export.";
                }
            }
        }

        private static string Escape(string value)
        {
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null)
                return;

            if (IsDarkMode)
            {
                app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
                app.Resources["WindowForegroundBrush"] = new SolidColorBrush(Colors.WhiteSmoke);
            }
            else
            {
                app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                app.Resources["WindowForegroundBrush"] = new SolidColorBrush(Colors.Black);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
