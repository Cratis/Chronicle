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

using var budget = new RunBudget(TimeSpan.FromMinutes(options.Profile.TimeoutMinutes));
RunBudget.Current = budget;

var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
var resultDirectory = Path.Combine(options.OutputDirectory, stamp);
Directory.CreateDirectory(resultDirectory);
await Console.Out.WriteLineAsync($"Results: {resultDirectory}");

var ledgers = new List<SettingsLedger>();
var exitCode = 0;
var cutShort = false;
try
{
    foreach (var provider in options.Providers)
    {
        var providerDirectory = Path.Combine(resultDirectory, StorageProviderNames.NameOf(provider));
        Directory.CreateDirectory(providerDirectory);
        StorageRunOptions.Current = options.ForProvider(provider);
        Measurements.Clear();
        if (cutShort || budget.Expired)
        {
            cutShort = true;
            await File.WriteAllTextAsync(Path.Combine(providerDirectory, "summary-part.md"), $"\n{StorageProviderNames.NameOf(provider)}: not run: budget exhausted\n");
            continue;
        }

        try
        {
            var host = await WithinBudget(ProviderHosts.Get(provider));
            var ledger = await host.BuildLedger(options, loadAtStart);
            ledgers.Add(ledger);
            await File.WriteAllTextAsync(Path.Combine(providerDirectory, "ledger.json"), ledger.ToJson());

            var job = Job.Default
                .WithToolchain(InProcessNoEmitToolchain.Default)
                .WithStrategy(RunStrategy.Monitoring)
                .WithLaunchCount(1)
                .WithWarmupCount(options.Profile.WarmupCount)
                .WithIterationCount(options.Profile.IterationCount);
            var config = ManualConfig.CreateEmpty()
                .AddJob(job)
                .AddFilter(new BenchmarkDotNet.Filters.SimpleFilter(benchmarkCase =>
                    benchmarkCase.Parameters.Items.FirstOrDefault(_ => _.Value is StorageProvider)?.Value is not StorageProvider measuredProvider
                    || KnownGaps.ReasonFor(measuredProvider, benchmarkCase.Descriptor.WorkloadMethod.Name) is null))
                .AddExporter(JsonExporter.Full)
                .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .WithArtifactsPath(Path.Combine(providerDirectory, "bdn"));
            var summaries = await WithinBudget(BenchmarkSwitcher.FromAssembly(typeof(StorageBenchmark).Assembly).RunAsync(["--filter", options.Filter], config).AsTask());
            cutShort = await WithinBudget(CountedPass.Run(typeof(StorageBenchmark).Assembly, options.Profile.IterationCount, options.Filter));
            budget.ThrowIfExpired();
            var verdict = RunVerdict.Of(summaries, Measurements.TotalErrors());
            if (!verdict.Succeeded)
            {
                await Console.Error.WriteLineAsync($"{StorageProviderNames.NameOf(provider)} run failed: {verdict.Reason}");
                exitCode = 1;
            }

            await WithinBudget(PlanCapture.Run(host, Math.Min(options.Profile.CatchUpCounts.Max(), 20_000), providerDirectory));
            budget.ThrowIfExpired();
        }
        catch (Exception error) when (budget.Expired || error.GetBaseException() is BudgetExceededException)
        {
            cutShort = true;
            await Console.Error.WriteLineAsync($"{StorageProviderNames.NameOf(provider)}: profile budget exhausted; partial results kept.");
        }
        catch (Exception error)
        {
            await Console.Error.WriteLineAsync($"{StorageProviderNames.NameOf(provider)} failed: {error}");
            exitCode = 1;
        }
        finally
        {
            Measurements.Write(Path.Combine(providerDirectory, "counts.json"));
            var scaling = ScalingReport.FromMeasurements();
            if (scaling.Length > 0) await File.WriteAllTextAsync(Path.Combine(providerDirectory, "scaling.md"), scaling);
            await File.WriteAllTextAsync(Path.Combine(providerDirectory, "summary-part.md"), RunSummary.ForProvider(StorageProviderNames.NameOf(provider), KnownGaps.For(provider)));
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

    await File.WriteAllTextAsync(Path.Combine(resultDirectory, "summary.md"), RunSummary.Combine(ledgers, resultDirectory, cutShort));
    await ProviderHosts.DisposeAll();
}

return cutShort ? 4 : exitCode;

async Task<T> WithinBudget<T>(Task<T> work)
{
    using var delayCancellation = new CancellationTokenSource();
    var backstop = Task.Delay(budget.Remaining + TimeSpan.FromSeconds(60), delayCancellation.Token);
    if (await Task.WhenAny(work, backstop) != work) throw new BudgetExceededException(TimeSpan.FromMinutes(options.Profile.TimeoutMinutes));
    await delayCancellation.CancelAsync();
    return await work;
}
