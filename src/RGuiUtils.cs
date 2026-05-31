using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace RGui;

public record ResultItem(string FilePath, int LineNumber, string Display);

// ObservableCollection with AddRange that fires a single Reset notification
// instead of one CollectionChanged per item, keeping the UI thread unblocked.
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Items.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

public static class RGuiUtils
{
    public static ResultItem? ParseLine(string line)
    {
        if (!line.StartsWith('{')) return null;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() != "match") return null;
            var data = root.GetProperty("data");
            var file = data.GetProperty("path").GetProperty("text").GetString() ?? "";
            var lineN = data.GetProperty("line_number").GetInt32();
            var text = data.GetProperty("lines").GetProperty("text")
                            .GetString()?.TrimEnd() ?? "";
            return new ResultItem(file, lineN,
                $"{Path.GetFileName(file)}:{lineN}  {text}");
        }
        catch { return null; }
    }

    public static void OpenFile(ResultItem r)
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
}
