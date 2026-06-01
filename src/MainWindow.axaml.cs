using System;
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

    // --- Event handlers ---

    private async void OnSearch(object? sender, RoutedEventArgs e)
    {
        var options = ReadSearchOptions();
        if (options is null) return;

        _results.Clear();
        _cts = new CancellationTokenSource();
        BeginSearch();

        var runner = new RipgrepRunner(options, _cts.Token);
        var flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        flushTimer.Tick += (_, _) => _results.AddRange(runner.DrainBatch(200));
        flushTimer.Start();

        var succeeded = false;
        try
        {
            await runner.RunAsync();
            succeeded = true;
        }
        catch (OperationCanceledException) { StatusText.Text = "Cancelled"; }
        catch (Exception ex) { StatusText.Text = $"Error: {ex.Message}"; }
        finally
        {
            flushTimer.Stop();
            EndSearch();
        }

        if (succeeded)
        {
            _results.AddRange(runner.DrainBatch());
            StatusText.Text = runner.WasCapped
                ? $"Showing first {RipgrepRunner.ResultCap:N0} matches — refine your pattern"
                : FormatMatchCount(runner.MatchCount);
        }
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

    // --- Helpers ---

    private SearchOptions? ReadSearchOptions()
    {
        var pattern = PatternBox.Text ?? "";
        return string.IsNullOrWhiteSpace(pattern) ? null
            : new SearchOptions(pattern, PathBox.Text ?? ".", CaseSensitiveCheck.IsChecked ?? true, RegexCheck.IsChecked ?? true);
    }

    private void BeginSearch()
    {
        SearchBtn.IsEnabled = false;
        CancelBtn.IsEnabled = true;
        StatusText.Text = "Searching";
        _animating = true;
        _ = AnimateDotsAsync();
    }

    private void EndSearch()
    {
        _animating = false;
        SearchBtn.IsEnabled = true;
        CancelBtn.IsEnabled = false;
    }

    private static string FormatMatchCount(int count) =>
        $"{count} {(count == 1 ? "match" : "matches")}";

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
