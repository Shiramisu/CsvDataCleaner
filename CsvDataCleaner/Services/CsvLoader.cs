using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvDataCleaner.Services
{
    public class CsvLoader
    {
        public DataTable LoadCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Pfad ist leer.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Datei nicht gefunden.", filePath);

            var lines = File.ReadAllLines(filePath, new UTF8Encoding(false));
            if (lines.Length == 0)
                throw new InvalidOperationException("Datei ist leer.");

            char separator = DetectSeparator(lines[0]);
            var table = new DataTable("CsvData");

            string[] headers = SplitLine(lines[0], separator);
            foreach (var h in headers)
            {
                string name = string.IsNullOrWhiteSpace(h) ? $"Spalte_{table.Columns.Count + 1}" : h.Trim();
                if (table.Columns.Contains(name))
                {
                    int i = 2;
                    string baseName = name;
                    while (table.Columns.Contains(name))
                    {
                        name = baseName + "_" + i;
                        i++;
                    }
                }
                table.Columns.Add(name, typeof(string));
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] parts = SplitLine(lines[i], separator);
                var row = table.NewRow();
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    string value = c < parts.Length ? parts[c] : string.Empty;
                    row[c] = value;
                }
                table.Rows.Add(row);
            }

            return table;
        }

        private static char DetectSeparator(string line)
        {
            int comma = line.Count(c => c == ',');
            int semicolon = line.Count(c => c == ';');
            return semicolon >= comma ? ';' : ',';
        }

        private static string[] SplitLine(string line, char sep) => line.Split(sep);
    }
}
