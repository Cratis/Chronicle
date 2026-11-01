// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Reads sampled allocation stacks from an existing trace without running another workload.
/// </summary>
internal static class SetupAllocationTrace
{
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Attributes allocation ticks to the previously recorded synchronous phase windows.
    /// </summary>
    /// <param name="trace">The recorded EventPipe trace.</param>
    /// <param name="measurements">The boundary measurement JSON.</param>
    /// <param name="output">The stack summary JSON.</param>
    /// <returns>Zero after the existing trace is processed.</returns>
    internal static int Run(string trace, string measurements, string output)
    {
        using var report = JsonDocument.Parse(File.ReadAllText(measurements));
        var windows = report.RootElement.GetProperty("Samples").EnumerateArray()
            .Where(sample => sample.GetProperty("Phase").GetString()!.StartsWith("warm.", StringComparison.Ordinal))
            .Select(sample => (Name: sample.GetProperty("Phase").GetString()!, Start: sample.GetProperty("StartUtc").GetDateTime(), End: sample.GetProperty("EndUtc").GetDateTime())).ToArray();
        var etlx = TraceLog.CreateFromEventPipeDataFile(trace);
        using var log = TraceLog.OpenOrConvert(etlx);
        var stacks = new Dictionary<string, long>(StringComparer.Ordinal);
        var methods = new Dictionary<string, long>(StringComparer.Ordinal);
        var types = new Dictionary<string, long>(StringComparer.Ordinal);
        var totals = windows.ToDictionary(window => window.Name, _ => 0L, StringComparer.Ordinal);
        var counts = windows.ToDictionary(window => window.Name, _ => 0, StringComparer.Ordinal);
        foreach (var traceEvent in log.Events)
        {
            if (traceEvent is not GCAllocationTickTraceData data) continue;
            var time = data.TimeStamp.ToUniversalTime();
            var window = windows.FirstOrDefault(window => time >= window.Start && time <= window.End);
            if (window.Name is null) continue;
            var bytes = data.AllocationAmount64;
            var frames = new List<string>();
            for (var stack = data.CallStack(); stack is not null; stack = stack.Caller)
            {
                frames.Add(stack.CodeAddress.FullMethodName);
            }
            var key = window.Name + "/" + string.Join('\n', frames);
            stacks[key] = stacks.GetValueOrDefault(key) + bytes;
            foreach (var method in frames.Distinct(StringComparer.Ordinal))
            {
                key = window.Name + "/" + method;
                methods[key] = methods.GetValueOrDefault(key) + bytes;
            }
            key = window.Name + "/" + data.TypeName;
            types[key] = types.GetValueOrDefault(key) + bytes;
            totals[window.Name] = totals.GetValueOrDefault(window.Name) + bytes;
            counts[window.Name] = counts.GetValueOrDefault(window.Name) + 1;
        }
        var summary = new
        {
            Trace = trace,
            log.EventsLost,
            SampledBytes = totals,
            SampleCounts = counts,
            Stacks = stacks.OrderByDescending(pair => pair.Value).Take(30).ToArray(),
            InclusiveMethods = methods.OrderByDescending(pair => pair.Value).Take(50).ToArray(),
            Types = types.OrderByDescending(pair => pair.Value).Take(30).ToArray(),
            Semantics = "Sampled allocation tick estimates; inclusive methods overlap; windows use measured UTC timestamps; not exact per-method allocation counters"
        };
        File.WriteAllText(output, JsonSerializer.Serialize(summary, _jsonOptions));
        Console.WriteLine($"setup-allocation-trace: output={output} lost={log.EventsLost} warm-windows={totals.Count}");
        return counts.GetValueOrDefault("warm.constructor") > 0 ? 0 : 1;
    }
}
