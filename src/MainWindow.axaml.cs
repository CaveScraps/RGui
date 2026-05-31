using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace RGui;

public partial class MainWindow : Window
{
    private readonly BulkObservableCollection<ResultItem> _results = new();
    private CancellationTokenSource? _cts;
    private bool _animating;

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

        var path = PathBox.Text ?? ".";
        var caseSensitive = CaseSensitiveCheck.IsChecked ?? true;
        var useRegex = RegexCheck.IsChecked ?? true;

        _results.Clear();

        // Local queue per search — abandoned queues are GC'd with no drain cost on the UI thread
        var pending = new ConcurrentQueue<ResultItem>();

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var count = 0;

        SearchBtn.IsEnabled = false;
        CancelBtn.IsEnabled = true;
        StatusText.Text = "Searching";
        _animating = true;
        _ = AnimateDotsAsync();

        var flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        flushTimer.Tick += (_, _) => FlushPending(pending);
        flushTimer.Start();

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
                if (!useRegex) proc.StartInfo.ArgumentList.Add("--fixed-strings");
                proc.StartInfo.ArgumentList.Add("--");
                proc.StartInfo.ArgumentList.Add(pattern);
                proc.StartInfo.ArgumentList.Add(path);

                proc.Start();
                try
                {
                    string? line;
                    while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
                    {
                        var item = RGuiUtils.ParseLine(line);
                        if (item is null) continue;
                        count++;
                        pending.Enqueue(item);
                    }
                    await proc.WaitForExitAsync(ct);
                }
                finally
                {
                    if (!proc.HasExited) proc.Kill(entireProcessTree: true);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Cancelled — stop the timer and drop the pending queue (no drain)
            flushTimer.Stop();
            _animating = false;
            SearchBtn.IsEnabled = true;
            CancelBtn.IsEnabled = false;
            StatusText.Text = "Cancelled";
            return;
        }
        catch (Exception ex)
        {
            flushTimer.Stop();
            _animating = false;
            StatusText.Text = $"Error: {ex.Message}";
            SearchBtn.IsEnabled = true;
            CancelBtn.IsEnabled = false;
            return;
        }

        flushTimer.Stop();

        // Flush whatever arrived after the last timer tick
        var tail = new List<ResultItem>();
        while (pending.TryDequeue(out var item))
            tail.Add(item);
        if (tail.Count > 0)
            _results.AddRange(tail);

        _animating = false;
        SearchBtn.IsEnabled = true;
        CancelBtn.IsEnabled = false;
        StatusText.Text = $"{count} {(count == 1 ? "match" : "matches")}";
    }

    private void FlushPending(ConcurrentQueue<ResultItem> pending)
    {
        var batch = new List<ResultItem>();
        while (pending.TryDequeue(out var item) && batch.Count < 200)
            batch.Add(item);
        if (batch.Count > 0)
            _results.AddRange(batch);
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
            RGuiUtils.OpenFile(r);
    }

    private async Task AnimateDotsAsync()
    {
        string[] frames = [".  ", ".. ", "..."];
        var i = 0;
        while (_animating)
        {
            DotsText.Text = frames[i++ % 3];
            await Task.Delay(400);
        }
        DotsText.Text = "";
    }
}
