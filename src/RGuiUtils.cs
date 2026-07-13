using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    // ripgrep is bundled alongside the executable; we always run that copy
    // (resolved by full path so it works regardless of PATH or platform).
    public static readonly string BundledRgPath =
        Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "rg.exe" : "rg");

    public static ResultItem? ParseLine(string line, string searchRoot)
    {
        if (!line.StartsWith('{'))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() != "match")
            {
                return null;
            }

            var data = root.GetProperty("data");
            var file = data.GetProperty("path").GetProperty("text").GetString() ?? "";
            var lineNumber = data.GetProperty("line_number").GetInt32();
            var text = data.GetProperty("lines").GetProperty("text").GetString()?.TrimEnd() ?? "";

            return new ResultItem(file, lineNumber,
                $"{Path.GetRelativePath(searchRoot, file)}:{lineNumber}",
                text);
        }
        catch (JsonException) { return null; }
        catch (KeyNotFoundException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public static void OpenFile(ResultItem r)
    {
        if (IsOnPath("code"))
        {
            var vsCodeStartInfo = new ProcessStartInfo("code") { UseShellExecute = false };
            vsCodeStartInfo.ArgumentList.Add("--goto");
            vsCodeStartInfo.ArgumentList.Add($"{r.FilePath}:{r.LineNumber}");
            Process.Start(vsCodeStartInfo);
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo(r.FilePath) { UseShellExecute = true });
            }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }
        }
    }

    public static bool IsOnPath(string command)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];

        IEnumerable<string> extensions;
        if (OperatingSystem.IsWindows())
        {
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
            if (pathExt is null)
            {
                // PATHEXT missing means we can't determine valid extensions;
                // safer to report not found than guess a fallback list.
                return false;
            }

            extensions = pathExt.Split(';');
        }
        else
        {
            extensions = [""];
        }

        foreach (var path in paths)
        {
            foreach (var ext in extensions)
            {
                if (File.Exists(Path.Combine(path, command + ext)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
