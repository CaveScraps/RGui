using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace RGui;

public record ResultItem(string FilePath, int LineNumber, string Display);

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ResultItem> _results = new();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        ResultsList.ItemsSource = _results;
        PathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private async void OnSearch(object? sender, RoutedEventArgs e)
    {
        var pattern = PatternBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(pattern)) return;

        // Snapshot UI state before going off-thread
        var path = PathBox.Text ?? ".";
        var caseSensitive = CaseSensitiveCheck.IsChecked ?? true;
        var useRegex = RegexCheck.IsChecked ?? true;

        _results.Clear();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var count = 0;

        SearchBtn.IsEnabled = false;
        CancelBtn.IsEnabled = true;
        StatusText.Text = "Searching…";

        try
        {
            await Task.Run(async () =>
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "rg";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("--json");
                if (!caseSensitive) proc.StartInfo.ArgumentList.Add("--ignore-case");
                if (!useRegex)      proc.StartInfo.ArgumentList.Add("--fixed-strings");
                proc.StartInfo.ArgumentList.Add("--");
                proc.StartInfo.ArgumentList.Add(pattern);
                proc.StartInfo.ArgumentList.Add(path);

                proc.Start();
                try
                {
                    string? line;
                    while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
                    {
                        var item = ParseLine(line);
                        if (item is null) continue;
                        count++;
                        Dispatcher.UIThread.Post(() => _results.Add(item));
                    }
                    await proc.WaitForExitAsync(ct);
                }
                finally
                {
                    if (!proc.HasExited) proc.Kill(entireProcessTree: true);
                }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            SearchBtn.IsEnabled = true;
            CancelBtn.IsEnabled = false;
            return;
        }

        SearchBtn.IsEnabled = true;
        CancelBtn.IsEnabled = false;
        StatusText.Text = ct.IsCancellationRequested
            ? "Cancelled"
            : $"{count} {(count == 1 ? "match" : "matches")}";
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => _cts?.Cancel();

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select search root", AllowMultiple = false });
        if (result.Count > 0)
            PathBox.Text = result[0].Path.LocalPath;
    }

    private void PatternBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && SearchBtn.IsEnabled)
            OnSearch(null, null!);
    }

    private void ResultsList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResultsList.SelectedItem is ResultItem r)
            OpenFile(r);
    }

    private static void OpenFile(ResultItem r)
    {
        // Try VS Code with --goto for line support; fall back to system default
        try
        {
            Process.Start(new ProcessStartInfo("code", $"--goto \"{r.FilePath}\":{r.LineNumber}")
                { UseShellExecute = false });
            return;
        }
        catch { }

        try { Process.Start(new ProcessStartInfo(r.FilePath) { UseShellExecute = true }); }
        catch { }
    }

    private static ResultItem? ParseLine(string line)
    {
        if (!line.StartsWith('{')) return null;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() != "match") return null;
            var data  = root.GetProperty("data");
            var file  = data.GetProperty("path").GetProperty("text").GetString() ?? "";
            var lineN = data.GetProperty("line_number").GetInt32();
            var text  = data.GetProperty("lines").GetProperty("text")
                            .GetString()?.TrimEnd() ?? "";
            return new ResultItem(file, lineN,
                $"{System.IO.Path.GetFileName(file)}:{lineN}  {text}");
        }
        catch { return null; }
    }
}
