// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Cratis.Chronicle.Benchmarks.Storage;

StorageRunOptions options;
try
{
    options = StorageRunOptions.Parse(args);
}
catch (ArgumentException error)
{
    await Console.Error.WriteLineAsync(error.Message);
    return 64;
}

var loadAtStart = MachineLoad.Current();
var preflight = PreflightResult.Check(options, loadAtStart, Environment.ProcessorCount);
if (!preflight.CanStart)
{
    await Console.Error.WriteLineAsync(preflight.Message);
    return preflight.ExitCode;
}

var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
var resultDirectory = Path.Combine(options.OutputDirectory, stamp);
Directory.CreateDirectory(resultDirectory);
await Console.Out.WriteLineAsync($"Results: {resultDirectory}");

var ledgers = new List<SettingsLedger>();
var exitCode = 0;
try
{
    foreach (var provider in options.Providers)
    {
        var providerDirectory = Path.Combine(resultDirectory, StorageProviderNames.NameOf(provider));
        Directory.CreateDirectory(providerDirectory);
        StorageRunOptions.Current = options.ForProvider(provider);
        Measurements.Clear();
        try
        {
            var host = await ProviderHosts.Get(provider);
            var ledger = await host.BuildLedger(options, loadAtStart);
            ledgers.Add(ledger);
            await File.WriteAllTextAsync(Path.Combine(providerDirectory, "ledger.json"), ledger.ToJson());

            var job = Job.Default
                .WithToolchain(InProcessNoEmitToolchain.Instance)
                .WithStrategy(RunStrategy.Monitoring)
                .WithLaunchCount(1)
                .WithWarmupCount(options.Profile.WarmupCount)
                .WithIterationCount(options.Profile.IterationCount);
            var config = ManualConfig.CreateEmpty()
                .AddJob(job)
                .AddExporter(JsonExporter.Full)
                .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .WithArtifactsPath(Path.Combine(providerDirectory, "bdn"));
            BenchmarkSwitcher.FromAssembly(typeof(StorageBenchmark).Assembly).Run(["--filter", options.Filter], config);
        }
        catch (Exception error)
        {
            await Console.Error.WriteLineAsync($"{StorageProviderNames.NameOf(provider)} failed: {error}");
            exitCode = 1;
        }
        finally
        {
            Measurements.Write(Path.Combine(providerDirectory, "counts.json"));
            await File.WriteAllTextAsync(Path.Combine(providerDirectory, "summary-part.md"), RunSummary.ForProvider(StorageProviderNames.NameOf(provider)));
        }
    }
}
finally
{
    var loadAtEnd = MachineLoad.Current();
    foreach (var ledger in ledgers)
    {
        await File.WriteAllTextAsync(Path.Combine(resultDirectory, ledger.Provider, "ledger.json"), ledger.WithLoadAtEnd(loadAtEnd).ToJson());
    }

    await File.WriteAllTextAsync(Path.Combine(resultDirectory, "summary.md"), RunSummary.Combine(ledgers, resultDirectory));
    await ProviderHosts.DisposeAll();
}

return exitCode;
