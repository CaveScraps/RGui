using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RGui;

public record SearchOptions(string Pattern, string Path, bool CaseSensitive, bool UseRegex);

public class RipgrepRunner
{
    private readonly SearchOptions _options;
    private readonly CancellationToken _ct;
    private readonly ConcurrentQueue<ResultItem> _pending = new();

    public int MatchCount { get; private set; }

    public RipgrepRunner(SearchOptions options, CancellationToken ct = default)
    {
        _options = options;
        _ct = ct;
    }

    public async Task RunAsync()
    {
        await Task.Run(async () =>
        {
            await foreach (var item in StreamAsync(_ct))
            {
                MatchCount++;
                _pending.Enqueue(item);
            }
        });
    }

    public IList<ResultItem> DrainBatch(int maxItems = int.MaxValue)
    {
        var batch = new List<ResultItem>();
        while (batch.Count < maxItems && _pending.TryDequeue(out var item))
            batch.Add(item);
        return batch;
    }

    private async IAsyncEnumerable<ResultItem> StreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        using var proc = new Process();
        proc.StartInfo.FileName = "rg";
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        proc.StartInfo.CreateNoWindow = true;

        proc.StartInfo.ArgumentList.Add("--json");
        if (!_options.CaseSensitive) proc.StartInfo.ArgumentList.Add("--ignore-case");
        if (!_options.UseRegex) proc.StartInfo.ArgumentList.Add("--fixed-strings");
        proc.StartInfo.ArgumentList.Add("--");
        proc.StartInfo.ArgumentList.Add(_options.Pattern);
        proc.StartInfo.ArgumentList.Add(_options.Path);

        proc.Start();
        try
        {
            string? line;
            while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
            {
                var item = RGuiUtils.ParseLine(line);
                if (item is not null) yield return item;
            }
            await proc.WaitForExitAsync(ct);
        }
        finally
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
        }
    }
}
