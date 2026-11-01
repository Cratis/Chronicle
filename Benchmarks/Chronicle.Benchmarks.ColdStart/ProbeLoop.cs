// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Starts fresh child processes running one probe scenario each and summarizes them.
/// </summary>
public static class ProbeLoop
{
    static readonly JsonSerializerOptions _indentedOptions = new() { WriteIndented = true };

    /// <summary>
    /// Runs the loop against a warm kernel and writes results/probe-&lt;scenario&gt;.json.
    /// </summary>
    /// <param name="scenario">The scenario name.</param>
    /// <param name="count">The number of child processes to start.</param>
    /// <returns>Process exit code.</returns>
    public static async Task<int> Run(string scenario, int count)
    {
        await using var kernel = new ColdStartKernel();
        await kernel.Start();

        var assembly = typeof(ProbeLoop).Assembly.Location;
        var runs = new List<Dictionary<string, double>>();
        var loadAtStart = Environment.GetEnvironmentVariable("COLDSTART_LOAD_NOTE");
        for (var i = 0; i < count; i++)
        {
            var start = new ProcessStartInfo("dotnet", $"\"{assembly}\" probe {scenario}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(start)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                await Console.Error.WriteLineAsync($"Probe {i} failed with exit code {process.ExitCode}: {error}");
                return 1;
            }

            runs.Add(JsonSerializer.Deserialize<Dictionary<string, double>>(output.Trim().Split('\n')[^1])!);
        }

        var summary = new Dictionary<string, object?>
        {
            ["scenario"] = scenario,
            ["count"] = count,
            ["loadNote"] = loadAtStart,
            ["metrics"] = runs[0].Keys.ToDictionary(
                key => key,
                key => Summarize(runs.Select(run => run[key]).Order().ToArray())),
            ["runs"] = runs,
        };

        Directory.CreateDirectory("results");
        var path = Path.Combine("results", $"probe-{scenario}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, _indentedOptions));
        await Console.Out.WriteLineAsync($"Wrote {Path.GetFullPath(path)}");
        return 0;
    }

    static Dictionary<string, double> Summarize(double[] sorted) => new()
    {
        ["median"] = Percentile(sorted, 0.5),
        ["p90"] = Percentile(sorted, 0.9),
        ["min"] = sorted[0],
    };

    static double Percentile(double[] sorted, double fraction)
    {
        var position = (sorted.Length - 1) * fraction;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }
}
