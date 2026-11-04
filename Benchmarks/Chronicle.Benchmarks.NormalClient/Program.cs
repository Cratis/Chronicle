// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Benchmarks.NormalClient;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

// Usage: <exe> <events> <projections> <iterations> <dispatches> <output.json>
// One process runs one configuration. Each iteration builds fresh Reducers/Projections instances (what a client does
// at startup) and measures discovery, dispatch and release separately.
if (args.Length != 5) return 2;
var eventCount = int.Parse(args[0], CultureInfo.InvariantCulture);
var projectionCount = int.Parse(args[1], CultureInfo.InvariantCulture);
var iterations = int.Parse(args[2], CultureInfo.InvariantCulture);
var dispatches = int.Parse(args[3], CultureInfo.InvariantCulture);

Progress($"configuration events={eventCount} projections={projectionCount} iterations={iterations} warmups=2 dispatches={dispatches}");
var catalog = Catalog.Create(eventCount, projectionCount);
await using var services = new ServiceCollection().BuildServiceProvider();
var activator = new ClientArtifactsActivator(services, NullLoggerFactory.Instance);
var batch = new[] { new EventAndContext(Activator.CreateInstance(catalog.Events[0])!, EventContext.Empty) };
var classicReducer = catalog.Reducers[0];
var classicModel = classicReducer.GetInterfaces().Single(_ => _.IsGenericType).GetGenericArguments()[0];

var samples = new List<Sample>();
for (var iteration = -2; iteration < iterations; iteration++)
{
    var label = iteration < 0 ? $"warmup={iteration + 3}/2" : $"iteration={iteration + 1}/{iterations}";
    Progress($"{label} start");
    var sample = await Measure(label);
    if (iteration >= 0) samples.Add(sample);
    Progress($"{label} end");
}

var result = new
{
    events = eventCount,
    projections = projectionCount,
    iterations,
    dispatches,
    reducerDiscovery = Summarize(samples.Select(_ => _.ReducerDiscovery)),
    projectionDiscovery = Summarize(samples.Select(_ => _.ProjectionDiscovery)),
    dispatch = Summarize(samples.Select(_ => _.Dispatch)),
    release = Summarize(samples.Select(_ => _.Release)),
    handlerCount = samples[0].Handlers
};
await File.WriteAllTextAsync(args[4], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return 0;

IEventStore CreateEventStore()
{
    var store = Substitute.For<IEventStore>();
    store.Name.Returns((EventStoreName)"normal-client");
    store.Namespace.Returns((EventStoreNamespaceName)"default");
    var observers = Substitute.For<IObservers>();
    var contracts = Substitute.For<IServices>();
    contracts.Observers.Returns(observers);
    var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
    ((IChronicleServicesAccessor)connection).Services.Returns(contracts);
    store.Connection.Returns(connection);
    store.FailedPartitions.Returns(Substitute.For<Cratis.Chronicle.Observation.IFailedPartitions>());
    return store;
}

async Task<Sample> Measure(string iteration)
{
    // Recorded lifecycle calls retain their delegate targets even after Release unsubscribes.
    // Keep substitutes iteration-local, outside every measured phase, so histories cannot accumulate.
    var eventTypes = Substitute.For<IEventTypes>();
    eventTypes.AllClrTypes.Returns(catalog.Events.ToImmutableList());
    eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => new EventType(call.Arg<Type>().Name, 1));
    var eventStore = CreateEventStore();
    var artifacts = Substitute.For<IClientArtifactsProvider>();
    artifacts.EventTypes.Returns(catalog.Events);
    artifacts.Reducers.Returns(catalog.Reducers);
    artifacts.Projections.Returns(catalog.Projections);
    var reducers = new Reducers(
        eventStore,
        artifacts,
        services,
        activator,
        new ReducerValidator(),
        eventTypes,
        new DefaultNamingPolicy(),
        new JsonSerializerOptions(),
        Substitute.For<IIdentityProvider>(),
        Substitute.For<IReducerObservers>(),
        Substitute.For<IActivitySource<Reducers>>(),
        NullLogger<Reducers>.Instance);
    var projections = new Projections(eventStore, eventTypes, artifacts, new DefaultNamingPolicy(), activator, new JsonSerializerOptions(), NullLogger<Projections>.Instance);
    var sample = new Sample
    {
        ReducerDiscovery = await Run($"{iteration} phase=reducerDiscovery", reducers.Discover),
        ProjectionDiscovery = await Run($"{iteration} phase=projectionDiscovery", projections.Discover),
        Handlers = reducers.GetAllHandlers().Count()
    };
    var invoker = new ReducerInvoker(eventTypes, activator, classicReducer, classicModel, "normal-model");
    sample.Dispatch = await Run($"{iteration} phase=dispatch", async () =>
    {
        for (var index = 0; index < dispatches; index++)
        {
            var reduced = await invoker.Invoke(services, batch, null);
            if (!reduced.IsSuccess) throw new InvalidOperationException("Reducer dispatch failed.");
        }
    });
    sample.Release = await Run($"{iteration} phase=release", () =>
    {
        reducers.Release();
        return Task.CompletedTask;
    });
    return sample;
}

static async Task<Cost> Run(string phase, Func<Task> work)
{
    Progress($"{phase} GC start");
    GC.Collect();
    GC.WaitForPendingFinalizers();
    Progress($"{phase} GC end");
    Progress($"{phase} timed-work start");
    var thread = GC.GetAllocatedBytesForCurrentThread();
    var process = GC.GetTotalAllocatedBytes(true);
    var watch = Stopwatch.StartNew();
    await work();
    watch.Stop();
    var cost = new Cost(GC.GetAllocatedBytesForCurrentThread() - thread, GC.GetTotalAllocatedBytes(true) - process, watch.Elapsed.TotalMilliseconds);
    Progress($"{phase} timed-work end");
    return cost;
}

static void Progress(string message) => Console.Error.WriteLine($"{DateTimeOffset.UtcNow:O} pid={Environment.ProcessId} {message}");

static object Summarize(IEnumerable<Cost> costs)
{
    var list = costs.ToList();
    return new
    {
        threadBytesMin = list.Min(_ => _.ThreadBytes),
        threadBytesMedian = Median(list.Select(_ => (double)_.ThreadBytes)),
        processBytesMedian = Median(list.Select(_ => (double)_.ProcessBytes)),
        millisecondsMedian = Median(list.Select(_ => _.Milliseconds)),
        millisecondsMin = list.Min(_ => _.Milliseconds)
    };
}

static double Median(IEnumerable<double> values)
{
    var sorted = values.Order().ToArray();
    return sorted[sorted.Length / 2];
}
