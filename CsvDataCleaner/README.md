# CSV Data Cleaner

Ein WPF-Tool zum Analysieren und Bereinigen von CSV-Dateien.  
Deluxe-Version mit Regelwerk, Issue-Liste, Auto-Fixes, Qualitäts-Score und Dark Mode.

## Features

- CSV laden (`,` oder `;`, Header-Erkennung)
- Duplikate, Pflichtfelder, Typ- und Range-Verstöße erkennen
- Regelwerk pro Spalte (Text/Numeric/Date, Min/Max)
- Auto-Fixes (Trimmen, erneute Analyse)
- Export von bereinigter CSV und Issues
- Qualitäts-Score in %

## Projektstruktur

- `App.xaml` / `MainWindow.xaml`: WPF UI
- `ViewModels/MainViewModel.cs`: Logik, Commands, Theme
- `Services/CsvLoader.cs`: CSV einlesen
- `Services/CsvCleaner.cs`: Analyse, Auto-Fixes, Score
- `Models/*`: `CsvIssue`, `ColumnRule`, `RuleType`
- `Infrastructure/RelayCommand.cs`: Command-Implementierung

## Start

1. Projekt in Visual Studio öffnen.
2. Konfiguration: `Debug` + `Any CPU`.
3. Starten (F5).
4. CSV laden, Regeln anpassen, analysieren und exportieren.
