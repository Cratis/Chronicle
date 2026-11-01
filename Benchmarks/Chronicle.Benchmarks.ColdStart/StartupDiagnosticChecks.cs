// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Types;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Deterministic self-checks for the startup diagnostic harness.
/// </summary>
internal static class StartupDiagnosticChecks
{
    /// <summary>
    /// Runs all diagnostic self-checks.
    /// </summary>
    /// <returns>Zero when every check passes.</returns>
    internal static async Task<int> Run()
    {
        try
        {
            Assert(StartupDiagnosticRecords.Union([(0, 10), (2, 8), (7, 15), (20, 25)]) == 20, "nested/overlap/disjoint interval union");
            Assert(StartupDiagnosticRecords.Union([]) == 0, "empty union");
            await CheckRecorder();
            foreach (var enabled in new[] { false, true })
            {
                StartupDiagnostics.Recorder = enabled ? new StartupDiagnosticRecorder(4096) : null;
                CheckProvider();
                await CheckSchemaCaches();
            }
            StartupDiagnostics.Recorder = null;
            var disabledControl = new StartupDiagnosticRecorder(4096);
            var disabledSchemas = Schemas();
            Assert(disabledControl.Snapshot().Length == 0, "disabled control produces no records");
            var recorder = new StartupDiagnosticRecorder(4096);
            StartupDiagnostics.Recorder = recorder;
            var enabledSchemas = Schemas();
            Assert(disabledSchemas.SequenceEqual(enabledSchemas), "enabled/disabled schema bytes, metadata/defaults and registry aliases");
            Assert(recorder.Dropped == 0 && recorder.Active == 0, "check buffer completeness");
            var factories = new[] { StartupOperation.EventSchemaFactory, StartupOperation.ReadModelSchemaFactory, StartupOperation.LegacySchemaFactory, StartupOperation.PreciseStructureFactory, StartupOperation.LegacyStructureFactory };
            Assert(factories.All(operation => recorder.Snapshot().Any(record => record.Operation == operation)), "all five cache factory hooks executed");
            Assert(recorder.Snapshot().Any(record => record.Operation == StartupOperation.RegistryBinding), "registry binding hook executed");
            var overflowing = new StartupDiagnosticRecorder(0);
            StartupDiagnostics.Recorder = overflowing;
            Assert(disabledSchemas.SequenceEqual(Schemas()), "overflow does not change schema results or throw into generation");
            Assert(overflowing.Dropped > 0 && overflowing.Active == 0 && overflowing.Snapshot().Length == 0, "overflow invalidation counters");
            StartupDiagnostics.Recorder = null;
            var manifest = StartupDiagnosticRunner.CreateManifest();
            Assert(manifest.Facets.Count == StartupDiagnosticCatalog.Facets.Length, "all catalog facets asserted including empty facets");
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new { Kind = "diagnostic-checks", Passed = true, FacetCounts = manifest.Facets.ToDictionary(facet => facet.Key, facet => facet.Value.Length) }));
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.ToString());
            return 1;
        }
        finally
        {
            StartupDiagnostics.Recorder = null;
        }
    }

    static async Task CheckRecorder()
    {
        var recorder = new StartupDiagnosticRecorder(16);
        using (var parent = recorder.Start(StartupOperation.StoreRegistration))
        {
            await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            {
                using var child = recorder.Start(StartupOperation.ParallelTypes);
                child?.Complete();
            })));
            parent?.Complete();
        }
        var records = recorder.Snapshot();
        var root = records.Single(record => record.Operation == StartupOperation.StoreRegistration);
        Assert(records.Where(record => record.Id != root.Id).All(record => record.ParentId == root.Id), "async parent IDs");
        Assert(StartupDiagnosticRecords.Union(records.Select(record => (record.Start, record.End))) == root.End - root.Start, "nested spans not double counted");
        using (recorder.Start(StartupOperation.EventTypesPreparation)) { }
        Assert(recorder.Snapshot()[^1].Outcome == 2, "failure outcome");
        var task = Task.FromException(new InvalidOperationException("diagnostic synthetic failure"));
        recorder.Start(StartupOperation.EventTypesSend)!.Observe(task);
        Assert(task.IsFaulted && recorder.Snapshot()[^1].Outcome == 2, "task fault is observed, not replaced/consumed");
        recorder.Start(StartupOperation.EventTypesSend)!.Observe(Task.FromCanceled(new CancellationToken(canceled: true)));
        Assert(recorder.Snapshot()[^1].Outcome == 3, "canceled transport outcome");
        CheckCompletionVersusCapture();
        var bounded = new StartupDiagnosticRecorder(1);
        using (var first = bounded.Start(StartupOperation.ProviderInitialization))
        {
            using var overflow = bounded.Start(StartupOperation.AssemblyDiscovery);
            Assert(overflow is null, "overflow drops without throwing");
            first?.Complete();
        }
        Assert(bounded.Dropped == 1 && bounded.Active == 0 && bounded.Snapshot().Length == 1, "bounded capacity/drop accounting");
    }

    static void CheckCompletionVersusCapture()
    {
        // Deterministic: an observed scope whose task is incomplete is Active and invalidates the capture.
        var recorder = new StartupDiagnosticRecorder(16);
        var gate = new TaskCompletionSource();
        recorder.Start(StartupOperation.EventTypesSend)!.Observe(gate.Task);
        var pending = recorder.Capture();
        Assert(pending.Active == 1 && !pending.Valid && pending.Records.Single().Outcome == 0, "pending observer invalidates capture");
        gate.SetResult();
        var done = recorder.Capture();
        Assert(done.Active == 0 && done.Valid && done.Records.Single().Outcome == 1, "completed observer publishes a valid record");

        // Concurrent: completions race captures with no sleeps; every capture must be internally consistent.
        const int count = 512;
        var stress = new StartupDiagnosticRecorder(count);
        var gates = Enumerable.Range(0, count).Select(_ => new TaskCompletionSource()).ToArray();
        foreach (var source in gates) stress.Start(StartupOperation.EventTypesSend)!.Observe(source.Task);
        using var start = new ManualResetEventSlim();
        var completer = Task.Run(() =>
        {
            start.Wait();
            foreach (var source in gates) source.SetResult();
        });
        start.Set();
        StartupDiagnosticSnapshot capture;
        do
        {
            capture = stress.Capture();
            Assert(capture.Records.Length == count && capture.Records.Count(record => record.Outcome == 0) == capture.Active, "capture never tears or omits unpublished slots");
            Assert(capture.Records.Where(record => record.Outcome != 0).All(record => record.End >= record.Start && record.Outcome == 1), "published records are complete");
            Assert(capture.Valid == (capture.Active == 0), "validity tracks active observers");
        }
        while (capture.Active > 0);
        completer.GetAwaiter().GetResult();
        Assert(capture.Valid && stress.Capture().Records.Select(record => record.Id).Distinct().Count() == count, "final capture valid with every record published once");
    }

    static void CheckProvider()
    {
        var discovery = new Discovery();
        var provider = new DefaultClientArtifactsProvider(discovery);
        var events = provider.EventTypes.ToArray();
        _ = StartupDiagnosticCatalog.Capture(provider);
        _ = provider.EventTypes.ToArray();
        Assert(discovery.Attempts == 1 && events.SequenceEqual([typeof(BenchmarkEvent)]), "eager first getter, repeated snapshot reuse");

        foreach (var failTypes in new[] { false, true })
        {
            var original = new InvalidOperationException("diagnostic discovery failure");
            discovery = new Discovery();
            provider = new DefaultClientArtifactsProvider(discovery);
            if (failTypes) discovery.ReadTypes = () => throw original;
            else discovery.OnInitialize = () => throw original;
            Assert(ReferenceEquals(CaptureFailure(() => _ = provider.EventTypes), original), "original discovery exception identity");
            discovery.OnInitialize = null;
            discovery.ReadTypes = null;
            Assert(provider.EventTypes.SequenceEqual([typeof(BenchmarkEvent)]) && discovery.Attempts == 2, "discovery retries after failure");
        }
        discovery = new Discovery();
        provider = new DefaultClientArtifactsProvider(discovery);
        discovery.OnInitialize = () => _ = provider.EventTypes;
        var reentrant = CaptureFailure(() => _ = provider.EventTypes);
        Assert(reentrant?.Message == "Client artifacts cannot be accessed reentrantly during initialization. Complete assembly discovery before accessing client artifacts.", "reentrant guard and unchanged failure text");
        discovery.OnInitialize = null;
        Assert(provider.EventTypes.SequenceEqual([typeof(BenchmarkEvent)]), "reentrant failure retry");
    }

    static async Task CheckSchemaCaches()
    {
        var generator = Generator();
        var first = generator.Generate(typeof(BenchmarkEvent));
        Assert(ReferenceEquals(first, generator.Generate(typeof(BenchmarkEvent))), "precise cache reference reuse");
        var legacy = generator.GenerateLegacyEventType(typeof(BenchmarkEvent));
        Assert(ReferenceEquals(legacy, generator.GenerateLegacyEventType(typeof(BenchmarkEvent))), "legacy cache reference reuse");
        var readModel = generator.GenerateForReadModel(typeof(BenchmarkReadModel));
        Assert(ReferenceEquals(readModel, generator.GenerateForReadModel(typeof(BenchmarkReadModel))), "read-model cache reference reuse");
        var before = StartupDiagnostics.Recorder?.Snapshot().Count(record => record.Operation == StartupOperation.EventSchemaFactory) ?? 0;
        generator = Generator();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            return generator.Generate(typeof(BenchmarkEvent));
        })).ToArray();
        gate.SetResult();
        var results = await Task.WhenAll(calls);
        Assert(results.All(result => ReferenceEquals(result, results[0])), "ConcurrentDictionary winner reference unchanged");
        if (StartupDiagnostics.Recorder is { } recorder)
        {
            var factories = recorder.Snapshot().Count(record => record.Operation == StartupOperation.EventSchemaFactory) - before;
            Assert(factories is >= 1 and <= 8, "racing factories are observed, not assumed to equal retained entries");
            _ = generator.Generate(typeof(BenchmarkEvent));
            Assert(recorder.Snapshot().Count(record => record.Operation == StartupOperation.EventSchemaFactory) - before == factories, "warm hit adds no factory execution");
        }
        var firstRegistry = new Registry("first-alias");
        var secondRegistry = new Registry("second-alias");
        var aliases = await Task.WhenAll(
            Task.Run(() => generator.Generate(typeof(Compensation), firstRegistry)),
            Task.Run(() => generator.Generate(typeof(Compensation), secondRegistry)));
        Assert(aliases[0].GetCompensationFor() == "first-alias" && aliases[1].GetCompensationFor() == "second-alias", "concurrent registry binding isolation");
    }

    static string[] Schemas()
    {
        var generator = Generator();
        var first = new Registry("first-alias");
        var second = new Registry("second-alias");
        var firstPrecise = generator.Generate(typeof(Compensation), first);
        var secondPrecise = generator.Generate(typeof(Compensation), second);
        var firstLegacy = generator.GenerateLegacyEventType(typeof(Compensation), first);
        var secondLegacy = generator.GenerateLegacyEventType(typeof(Compensation), second);
        Assert(firstPrecise.GetCompensationFor() == "first-alias" && firstLegacy.GetCompensationFor() == "first-alias", "first registry alias");
        Assert(secondPrecise.GetCompensationFor() == "second-alias" && secondLegacy.GetCompensationFor() == "second-alias", "second registry alias");
        Assert(generator.Generate(typeof(Compensation), first).GetCompensationFor() == "first-alias", "binding cache isolation");
        Assert(!ReferenceEquals(firstPrecise, secondPrecise), "no decorated schema sharing between registries");
        Assert(JsonNode.Parse(firstPrecise.ToJson())!["properties"]!["Value"]!["type"]!.GetValue<string>() == "string", "precise default property shape");
        return [generator.Generate(typeof(BenchmarkEvent)).ToJson(), generator.GenerateLegacyEventType(typeof(BenchmarkEvent)).ToJson(), generator.GenerateForReadModel(typeof(BenchmarkReadModel)).ToJson(), firstPrecise.ToJson(), secondPrecise.ToJson(), firstLegacy.ToJson(), secondLegacy.ToJson()];
    }

    static JsonSchemaGenerator Generator() => new(new ComplianceMetadataResolver(new KnownInstancesOf<ICanProvideComplianceMetadataForType>(), new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()), new DefaultNamingPolicy());

    static Exception? CaptureFailure(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    static void Assert(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException($"Diagnostic check failed: {check}.");
    }

    sealed class Discovery : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [typeof(BenchmarkEvent).Assembly];
        public IEnumerable<Type> DefinedTypes => ReadTypes?.Invoke() ?? [typeof(BenchmarkEvent)];
        internal Action? OnInitialize { get; set; }
        internal Func<IEnumerable<Type>>? ReadTypes { get; set; }
        internal int Attempts { get; private set; }

        public void Initialize()
        {
            Attempts++;
            OnInitialize?.Invoke();
        }
    }

    sealed class Registry(string alias) : IEventTypes
    {
        public IImmutableList<Type> AllClrTypes => ImmutableList.Create(typeof(PlainEvent));
        public IImmutableList<EventType> All => ImmutableList.Create(new EventType(alias, 1));
        public Task Discover() => Task.CompletedTask;
        public Task Register() => Task.CompletedTask;
        public bool HasFor(EventTypeId eventTypeId) => eventTypeId.Value == alias;
        public bool HasFor(Type clrType) => clrType == typeof(PlainEvent);
        public Type GetClrTypeFor(EventTypeId eventTypeId) => typeof(PlainEvent);
        public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => typeof(PlainEvent);
        public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => throw new NotSupportedException();
        public EventType GetEventTypeFor(Type clrType) => new(alias, 1);
    }

    /// <summary>
    /// Plain event used by the registry check.
    /// </summary>
    /// <param name="Value">The value.</param>
    internal sealed record PlainEvent(string Value);

    /// <summary>
    /// Compensation for <see cref="PlainEvent"/> used by the registry check.
    /// </summary>
    /// <param name="Value">The value.</param>
    [CompensationFor<PlainEvent>]
    internal sealed record Compensation(string Value = "default");
}
