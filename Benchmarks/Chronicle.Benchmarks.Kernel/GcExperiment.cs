// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Runs the GC settings experiment matrix: one <see cref="GcWorkload"/> process per configuration and repeat, one setting changed at a time from the kernel's production configuration.
/// </summary>
/// <remarks>
/// <para>
/// Every configuration is a child process, because a GC setting cannot be changed in a running process. The settings
/// travel as DOTNET_GC* environment variables, which override the runtime configuration of the harness. The baseline
/// is what the kernel ships: Server GC with GCConserveMemory 5. --hard-limit-mb applies a heap hard limit to every
/// configuration; production runs under about 6.1 GB in an 8 GiB container, and Server GC behaves very differently with and
/// without a limit, so set it to compare like with like.
/// </para>
/// <para>
/// The table shows the median of the repeats. A configuration earns a default change only when its committed memory after
/// settling is at least --committed-gain percent lower than the baseline and its throughput is not more than
/// --regression percent lower. Throughput noise on a loaded machine is larger than that threshold: rerun on a quiet
/// machine with more --repeats before acting on a verdict.
/// </para>
/// </remarks>
public static class GcExperiment
{
    /// <summary>
    /// Runs the experiment.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Run(string[] args)
    {
        var repeats = Math.Max(1, ObserverFootprint.IntArgument(args, "--repeats", 3));
        var gain = ObserverFootprint.IntArgument(args, "--committed-gain", 20);
        var regression = ObserverFootprint.IntArgument(args, "--regression", 5);
        var hardLimitMb = ObserverFootprint.IntArgument(args, "--hard-limit-mb", 0);
        var only = StringArgument(args, "--only")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] passThroughNames = ["--namespaces", "--count", "--window", "--settle"];
        var passThrough = passThroughNames
            .Where(args.Contains)
            .SelectMany(name => new[] { name, args[Array.IndexOf(args, name) + 1] })
            .ToArray();

        var configurations = Configurations(hardLimitMb, StringArgument(args, "--affinitize-mask"))
            .Where(configuration => Selected(only, configuration.Name))
            .ToArray();
        if (configurations.Length == 0)
        {
            await Console.Error.WriteLineAsync("--only selected no configuration");
            return 1;
        }

        await Console.Out.WriteLineAsync($"GC experiment: {configurations.Length} configurations x {repeats} repeats, machine {Environment.ProcessorCount} cores");
        var medians = new List<(Configuration Configuration, GcRunResult Result)>();
        foreach (var configuration in configurations)
        {
            var results = new List<GcRunResult>();
            for (var repeat = 0; repeat < repeats; repeat++)
            {
                await Console.Out.WriteLineAsync($"  running {configuration.Name} ({repeat + 1}/{repeats}): {configuration.Describe()}");
                var result = await RunChild(configuration, passThrough);
                if (result is null)
                {
                    await Console.Error.WriteLineAsync($"  {configuration.Name} failed");
                    return 1;
                }

                await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"    committed {Mib(result.CommittedAfterSettling):F0} MiB, live {Mib(result.LiveAfterForcedCollection):F0} MiB, {result.OperationsPerSecond:F0} operations/s"));
                results.Add(result);
            }

            medians.Add((configuration, Median(results)));
        }

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(Table(medians, gain, regression));
        return 0;
    }

    static List<Configuration> Configurations(int hardLimitMb, string? affinitizeMask)
    {
        var baseline = new Dictionary<string, string>
        {
            ["DOTNET_gcServer"] = "1",
            ["DOTNET_GCConserveMemory"] = Hex(5),
        };
        if (hardLimitMb > 0)
        {
            baseline["DOTNET_GCHeapHardLimit"] = Hex((long)hardLimitMb * 1024 * 1024);
        }

        Configuration Vary(string name, params (string Key, string Value)[] changes)
        {
            var settings = new Dictionary<string, string>(baseline);
            foreach (var (key, value) in changes)
            {
                settings[key] = value;
            }

            return new(name, settings);
        }

        var configurations = new List<Configuration>
        {
            new("baseline", baseline),
            Vary("heap-count-4", ("DOTNET_GCHeapCount", Hex(4))),
            Vary("conserve-7", ("DOTNET_GCConserveMemory", Hex(7))),
            Vary("conserve-9", ("DOTNET_GCConserveMemory", Hex(9))),
            Vary("retain-vm-off", ("DOTNET_GCRetainVM", "0")),
            Vary("concurrent-off", ("DOTNET_gcConcurrent", "0")),
            Vary("datas-off", ("DOTNET_GCDynamicAdaptationMode", "0")),
            Vary("workstation", ("DOTNET_gcServer", "0")),
        };

        if (hardLimitMb == 0)
        {
            configurations.Add(Vary("hard-limit-75-percent", ("DOTNET_GCHeapHardLimitPercent", Hex(75))));
        }

        if (affinitizeMask is not null)
        {
            configurations.Add(Vary("affinitize-mask", ("DOTNET_GCHeapAffinitizeMask", affinitizeMask)));
        }

        return configurations;
    }

    static GcRunResult Median(List<GcRunResult> results)
    {
        long Pick(Func<GcRunResult, long> select) => results.Select(select).Order().ElementAt(results.Count / 2);
        double PickDouble(Func<GcRunResult, double> select) => results.Select(select).Order().ElementAt(results.Count / 2);
        int PickInt(Func<GcRunResult, int> select) => results.Select(select).Order().ElementAt(results.Count / 2);

        return new GcRunResult(
            results[0].ServerGc,
            Pick(_ => _.Operations),
            PickDouble(_ => _.LoadSeconds),
            Pick(_ => _.AllocatedBytes),
            PickDouble(_ => _.PauseMilliseconds),
            PickInt(_ => _.Gen0Collections),
            PickInt(_ => _.Gen1Collections),
            PickInt(_ => _.Gen2Collections),
            PickInt(_ => _.JsonOptionsAfterLoad),
            PickInt(_ => _.JsonOptionsLive),
            Pick(_ => _.CommittedAfterSettling),
            Pick(_ => _.ResidentAfterSettling),
            Pick(_ => _.HeapAfterSettling),
            Pick(_ => _.Gen2FragmentationAfterSettling),
            Pick(_ => _.LiveAfterForcedCollection),
            Pick(_ => _.CommittedAfterForcedCollection));
    }

    static string Table(List<(Configuration Configuration, GcRunResult Result)> rows, int gain, int regression)
    {
        var baseline = rows[0].Result;
        var table = new StringBuilder();
        table
            .AppendLine("| Configuration | Committed after settling (MiB) | Resident (MiB) | Live (MiB) | Gen2 fragmentation (MiB) | Allocation rate (MiB/s) | GC pause (ms) | Gen0/1/2 | Operations/s | JsonSerializerOptions after load / live | Committed vs baseline | Throughput vs baseline | Verdict |")
            .AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | --- |");
        foreach (var (configuration, result) in rows)
        {
            var committedChange = Percent(result.CommittedAfterSettling, baseline.CommittedAfterSettling);
            var throughputChange = Percent(result.OperationsPerSecond, baseline.OperationsPerSecond);
            var verdict = Verdict(configuration.Name, committedChange, throughputChange, gain, regression);
            table.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {configuration.Name} | {Mib(result.CommittedAfterSettling):F0} | {Mib(result.ResidentAfterSettling):F0} | {Mib(result.LiveAfterForcedCollection):F0} | {Mib(result.Gen2FragmentationAfterSettling):F0} | {result.AllocationRate / 1048576.0:F0} | {result.PauseMilliseconds:F0} | {result.Gen0Collections}/{result.Gen1Collections}/{result.Gen2Collections} | {result.OperationsPerSecond:F0} | {result.JsonOptionsAfterLoad} / {result.JsonOptionsLive} | {committedChange:+0;-0;0} % | {throughputChange:+0;-0;0} % | {verdict} |"));
        }

        return table.ToString();
    }

    static bool Selected(string[]? only, string name)
    {
        if (only is null)
        {
            return true;
        }

        return only.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    static string Verdict(string name, double committedChange, double throughputChange, int gain, int regression)
    {
        if (name == "baseline")
        {
            return "-";
        }

        return committedChange <= -gain && throughputChange >= -regression ? "candidate" : "document only";
    }

    static double Mib(long bytes) => bytes / 1048576.0;

    static double Percent(double value, double baseline) => baseline == 0 ? 0 : (value - baseline) / baseline * 100.0;

    static string Hex(long value) => value.ToString("X", CultureInfo.InvariantCulture);

    static string? StringArgument(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    static async Task<GcRunResult?> RunChild(Configuration configuration, string[] passThrough)
    {
        var assembly = typeof(GcExperiment).Assembly.Location;
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add("--gc-workload");
        foreach (var argument in passThrough)
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var (key, value) in configuration.Settings)
        {
            start.Environment[key] = value;
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("The child process did not start");
        var error = process.StandardError.ReadToEndAsync();
        GcRunResult? result = null;
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            if (line.StartsWith(GcWorkload.ResultPrefix, StringComparison.Ordinal))
            {
                result = JsonSerializer.Deserialize<GcRunResult>(line[GcWorkload.ResultPrefix.Length..]);
            }
        }

        await process.WaitForExitAsync();
        if (process.ExitCode != 0 || result is null)
        {
            await Console.Error.WriteLineAsync(await error);
            return null;
        }

        return result;
    }

    /// <summary>
    /// A named GC configuration, as the environment variables that select it.
    /// </summary>
    /// <param name="Name">The name of the configuration.</param>
    /// <param name="Settings">The environment variables, hexadecimal as the runtime expects.</param>
    internal sealed record Configuration(string Name, IReadOnlyDictionary<string, string> Settings)
    {
        /// <summary>
        /// Describes the settings on one line.
        /// </summary>
        /// <returns>The description.</returns>
        public string Describe() => string.Join(' ', Settings.Select(setting => $"{setting.Key}={setting.Value}"));
    }
}
