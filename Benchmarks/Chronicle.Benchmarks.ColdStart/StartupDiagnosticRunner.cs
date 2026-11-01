// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Runs the local startup diagnostic smoke and its child processes.
/// </summary>
internal static class StartupDiagnosticRunner
{
    /// <summary>
    /// The pinned kernel image required for the diagnostic.
    /// </summary>
    internal const string KernelImage = "cratis/chronicle@sha256:390fbf209510e87ea7e5844238744e66609179d9bae6db2505e806b11e4f3f1e";
    const int Capacity = 65536;

    /// <summary>
    /// Runs the diagnostic command identified by the arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> Run(string[] args)
    {
        if (args is ["diagnostic-checks"]) return await StartupDiagnosticChecks.Run();
        if (args is ["diagnostic-child", var scenario, var control, var manifestPath]) return await Child(scenario, control, manifestPath);
        if (args is not ["diagnostic-pairs", "--pairs", "1", "--trials", "cold,warm", "--controls", "instrumented,uninstrumented", "--seed", var seedText])
        {
            await Console.Error.WriteLineAsync("Local diagnostic smoke only: diagnostic-pairs --pairs 1 --trials cold,warm --controls instrumented,uninstrumented --seed <integer>.");
            return 2;
        }
        if (Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") != KernelImage)
        {
            await Console.Error.WriteLineAsync($"Set CRATIS_CHRONICLE_LOCAL_IMAGE={KernelImage}.");
            return 2;
        }
        var output = Environment.GetEnvironmentVariable("CHRONICLE_STARTUP_DIAGNOSTIC_OUTPUT");
        if (string.IsNullOrEmpty(output) || !Path.GetFullPath(output).Contains("/.ai-work/", StringComparison.Ordinal))
        {
            await Console.Error.WriteLineAsync("Set CHRONICLE_STARTUP_DIAGNOSTIC_OUTPUT to the task directory under .ai-work/.");
            return 2;
        }
        var seed = int.Parse(seedText);
        var manifest = CreateManifest();
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, $"catalog-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest));
        var smokeStart = Stopwatch.GetTimestamp();
        await using var kernel = new ColdStartKernel();
        await kernel.Start();
        var random = new Random(seed);
        var order = new[] { ("auto", "instrumented"), ("explicit", "instrumented"), ("auto", "uninstrumented"), ("explicit", "uninstrumented") };
        random.Shuffle(order);
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new { Kind = "diagnostic-smoke-order", Seed = seed, Order = order.Select(pair => new { Scenario = pair.Item1, Control = pair.Item2 }), Environment = StartupDiagnosticRecords.EnvironmentInfo() }));
        foreach (var (runScenario, runControl) in order)
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(StartupDiagnosticRunner).Assembly.Location);
            foreach (var argument in new[] { "diagnostic-child", runScenario, runControl, path }) start.ArgumentList.Add(argument);
            using var child = Process.Start(start)!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            await child.WaitForExitAsync();
            var text = await stdout;
            await File.WriteAllTextAsync(Path.Combine(output, $"smoke-{runScenario}-{runControl}-{child.Id}.jsonl"), text);
            await Console.Out.WriteAsync(text);
            await Console.Error.WriteAsync(await stderr);
            if (child.ExitCode != 0) return child.ExitCode;
        }
        var smokeSeconds = Stopwatch.GetElapsedTime(smokeStart).TotalSeconds;
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new
        {
            Kind = "smoke-budget-only-not-performance",
            SmokeSeconds = smokeSeconds,
            CandidateFullRunPairs = 20,
            RoughUpperBudgetSeconds = smokeSeconds * 20,
            Note = "Includes kernel setup; single smoke cannot calibrate uncertainty or overhead. Full runs remain unauthorized."
        }));
        return 0;
    }

    /// <summary>
    /// Creates the manifest from the automatic provider after asserting parity with the explicit catalog.
    /// </summary>
    /// <returns>The manifest.</returns>
    internal static Manifest CreateManifest()
    {
        var facets = StartupDiagnosticCatalog.Capture(DefaultClientArtifactsProvider.Default);
        StartupDiagnosticCatalog.AssertParity(DefaultClientArtifactsProvider.Default, StartupDiagnosticCatalog.Resolve(facets));
        return new(
            facets,
            typeof(StartupDiagnosticRunner).Assembly.ManifestModule.ModuleVersionId,
            typeof(ChronicleClient).Assembly.ManifestModule.ModuleVersionId,
            StartupDiagnosticRecords.Hash(typeof(StartupDiagnosticRunner).Assembly.Location),
            StartupDiagnosticRecords.Hash(typeof(ChronicleClient).Assembly.Location),
            RegistrationMode.Inline,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString());
    }

    static async Task<int> Child(string scenario, string control, string manifestPath)
    {
        if (scenario is not ("auto" or "explicit") || control is not ("instrumented" or "uninstrumented")) return 2;
        var manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(manifestPath))!;
        if (manifest.HarnessMvid != typeof(StartupDiagnosticRunner).Assembly.ManifestModule.ModuleVersionId ||
            manifest.ClientMvid != typeof(ChronicleClient).Assembly.ManifestModule.ModuleVersionId ||
            manifest.HarnessHash != StartupDiagnosticRecords.Hash(typeof(StartupDiagnosticRunner).Assembly.Location) ||
            manifest.ClientHash != StartupDiagnosticRecords.Hash(typeof(ChronicleClient).Assembly.Location) ||
            manifest.RegistrationMode != RegistrationMode.Inline ||
            manifest.Runtime != RuntimeInformation.FrameworkDescription ||
            manifest.Architecture != RuntimeInformation.ProcessArchitecture.ToString())
        {
            throw new InvalidOperationException("Diagnostic artifact/registration-mode parity failed.");
        }
        var environment = StartupDiagnosticRecords.EnvironmentInfo();
        IClientArtifactsProvider? explicitCatalog = null;
        for (var trial = 0; trial < 2; trial++)
        {
            var recorder = control == "instrumented" ? new StartupDiagnosticRecorder(Capacity) : null;
            StartupDiagnostics.Recorder = recorder;
            Exception? failure = null;
            ChronicleClient? client = null;
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var start = Stopwatch.GetTimestamp();
            try
            {
                if (scenario == "explicit") explicitCatalog ??= StartupDiagnosticCatalog.Resolve(manifest.Facets);
                var options = new ChronicleOptions(
                    connectionString: new ChronicleConnectionString(Environment.GetEnvironmentVariable(ColdStartKernel.EnvironmentVariable)!),
                    autoDiscoverAndRegister: true,
                    connectTimeout: 30) { RegistrationMode = manifest.RegistrationMode };
                client = new ChronicleClient(options, artifactsProvider: explicitCatalog);
                _ = await client.GetEventStore(ColdStartKernel.EventStoreName);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            var end = Stopwatch.GetTimestamp();
            var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
            StartupDiagnostics.Recorder = null;

            // No serialization, hashing, console output, or disposal inside the readiness boundary.
            var snapshot = recorder?.Capture();
            var description = StartupDiagnosticRecords.Describe(snapshot, start, end);
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new
            {
                Kind = "startup-diagnostic",
                Process = Environment.ProcessId,
                Scenario = scenario,
                Control = control,
                Trial = trial == 0 ? "fresh-process-cold" : "within-process-warm",
                CacheState = trial == 0 ? "provider cold; schema caches new per client" : "provider/catalog retained; schema caches new per client",
                ServerStatePolicy = "one shared pinned kernel; existing ColdStartKernel prewarm registers artifacts and appends one warm event before all trials; diagnostic children append nothing",
                Boundary = "catalog resolution/options/client construction through awaited GetEventStore; excludes recorder preallocation, dump, disposal and append",
                AllocationBoundary = "process-wide precise total allocated bytes, immediately around readiness; includes instrumentation and concurrent process work",
                AllocatedBytes = allocatedAfter - allocatedBefore,
                Failure = failure?.ToString(),
                Environment = environment,
                Attribution = description
            }));
            client?.Dispose();
            if (failure is not null || snapshot is { Valid: false }) return 1;
            if (scenario == "auto") StartupDiagnosticCatalog.AssertParity(DefaultClientArtifactsProvider.Default, StartupDiagnosticCatalog.Resolve(manifest.Facets));
        }
        return 0;
    }

    /// <summary>
    /// The manifest passed from the host to explicit diagnostic children.
    /// </summary>
    /// <param name="Facets">The artifact type names per facet.</param>
    /// <param name="HarnessMvid">The harness module version id.</param>
    /// <param name="ClientMvid">The client module version id.</param>
    /// <param name="HarnessHash">The harness assembly hash.</param>
    /// <param name="ClientHash">The client assembly hash.</param>
    /// <param name="RegistrationMode">The registration mode.</param>
    /// <param name="Runtime">The runtime description.</param>
    /// <param name="Architecture">The process architecture.</param>
    internal sealed record Manifest(Dictionary<string, string[]> Facets, Guid HarnessMvid, Guid ClientMvid, string HarnessHash, string ClientHash, RegistrationMode RegistrationMode, string Runtime, string Architecture);
}
