using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace RGui;

public record ResultItem(string FilePath, int LineNumber, string PathPart, string MatchText);

// AddRange fires a single Reset notification instead of one per item,
// keeping the UI thread from being hammered during bulk updates.
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IList<T> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

public static class RGuiUtils
{
    public static ResultItem? ParseLine(string line, string searchRoot)
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
                $"{Path.GetRelativePath(searchRoot, file)}:{lineN}",
                text);
        }
        catch { return null; }
    }

    public static void OpenFile(ResultItem r)
    {
        // Try VS Code with --goto for line support; fall back to system default
        try
        {
            var vsCodeStartInfo = new ProcessStartInfo("code") { UseShellExecute = false };
            vsCodeStartInfo.ArgumentList.Add("--goto");
            vsCodeStartInfo.ArgumentList.Add($"{r.FilePath}:{r.LineNumber}");
            Process.Start(vsCodeStartInfo);
            return;
        }
        catch { }

        try { Process.Start(new ProcessStartInfo(r.FilePath) { UseShellExecute = true }); }
        catch { }
    }
}
