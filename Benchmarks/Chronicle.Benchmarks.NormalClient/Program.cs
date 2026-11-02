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

var catalog = Catalog.Create(eventCount, projectionCount);
await using var services = new ServiceCollection().BuildServiceProvider();
var activator = new ClientArtifactsActivator(services, NullLoggerFactory.Instance);
var eventTypes = Substitute.For<IEventTypes>();
eventTypes.AllClrTypes.Returns(catalog.Events.ToImmutableList());
eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => new EventType(call.Arg<Type>().Name, 1));
var eventStore = CreateEventStore();
var artifacts = Substitute.For<IClientArtifactsProvider>();
artifacts.EventTypes.Returns(catalog.Events);
artifacts.Reducers.Returns(catalog.Reducers);
artifacts.Projections.Returns(catalog.Projections);
var batch = new[] { new EventAndContext(Activator.CreateInstance(catalog.Events[0])!, EventContext.Empty) };
var classicReducer = catalog.Reducers[0];
var classicModel = classicReducer.GetInterfaces().Single(_ => _.IsGenericType).GetGenericArguments()[0];

var samples = new List<Sample>();
for (var iteration = -2; iteration < iterations; iteration++)
{
    var sample = await Measure();
    if (iteration >= 0) samples.Add(sample);
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

async Task<Sample> Measure()
{
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
        ReducerDiscovery = await Run(reducers.Discover),
        ProjectionDiscovery = await Run(projections.Discover),
        Handlers = reducers.GetAllHandlers().Count()
    };
    var invoker = new ReducerInvoker(eventTypes, activator, classicReducer, classicModel, "normal-model");
    sample.Dispatch = await Run(async () =>
    {
        for (var index = 0; index < dispatches; index++)
        {
            var reduced = await invoker.Invoke(services, batch, null);
            if (!reduced.IsSuccess) throw new InvalidOperationException("Reducer dispatch failed.");
        }
    });
    sample.Release = await Run(() =>
    {
        reducers.Release();
        return Task.CompletedTask;
    });
    return sample;
}

static async Task<Cost> Run(Func<Task> work)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    var thread = GC.GetAllocatedBytesForCurrentThread();
    var process = GC.GetTotalAllocatedBytes(true);
    var watch = Stopwatch.StartNew();
    await work();
    watch.Stop();
    return new(GC.GetAllocatedBytesForCurrentThread() - thread, GC.GetTotalAllocatedBytes(true) - process, watch.Elapsed.TotalMilliseconds);
}

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
