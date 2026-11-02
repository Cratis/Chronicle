// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The unconstrained client driving append and reactor work across isolated pairs.</summary>
internal static class ProfileWorkload
{
    /// <summary>Registers, seeds, appends and waits for distinct observer deliveries.</summary>
    /// <param name="context">The private run context.</param>
    /// <param name="endpoint">The mapped client endpoint.</param>
    /// <param name="settings">The workload scale.</param>
    /// <param name="sample">The phase-boundary sampling callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The measured workload result.</returns>
    /// <exception cref="HarnessFailure">Observers do not catch up before the deadline.</exception>
    public static async Task<WorkloadResult> Run(RunContext context, string endpoint, WorkloadSettings settings, Func<string, Task> sample, CancellationToken cancellationToken)
    {
        ProfileReactor.Reset();
        var address = new Uri($"https://{endpoint}");
        var connection = new ChronicleConnectionStringBuilder { Host = address.Host, Port = address.Port, Username = "container-profile", Password = context.ClientSecret, SkipTlsValidation = true }.Build();
        using var client = new ChronicleClient(new ChronicleOptions(connectionString: new ChronicleConnectionString(connection), connectTimeout: 30), artifactsProvider: new ObserverScopedClientArtifactsProvider(typeof(ProfileReactor)), loggerFactory: NullLoggerFactory.Instance);
        var stores = await Register(client, settings, cancellationToken);
        await Append(stores, settings.SeedEvents, settings, cancellationToken);
        await WaitForObservers(stores, settings.SeedEvents, cancellationToken);
        await sample("after-seed");
        var watch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            await Append(stores, settings.Events, settings, timeout.Token);
            var appendSeconds = watch.Elapsed.TotalSeconds;
            await WaitForObservers(stores, settings.SeedEvents + settings.Events, timeout.Token);
            watch.Stop();
            await sample("after-measurement");
            return new WorkloadResult(settings.Events * stores.Length, stores.Sum(store => ProfileReactor.Count(store.Name.ToString(), store.Namespace.ToString())), appendSeconds, watch.Elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HarnessFailure("notCaughtUp");
        }
    }

    static async Task<IEventStore[]> Register(ChronicleClient client, WorkloadSettings settings, CancellationToken cancellationToken)
    {
        var stores = new List<IEventStore>();
        for (var store = 0; store < settings.Stores; store++)
        {
            for (var ns = 0; ns < settings.Namespaces; ns++)
            {
                var eventStore = await client.GetEventStore($"profile-{store}", $"namespace-{ns}").WaitAsync(cancellationToken);
                await eventStore.DiscoverAll().WaitAsync(cancellationToken);
                await eventStore.RegisterAll().WaitAsync(cancellationToken);
                await eventStore.Reactors.WaitTillSubscribed<ProfileReactor>().WaitAsync(cancellationToken);
                stores.Add(eventStore);
            }
        }
        return stores.ToArray();
    }

    static Task Append(IEventStore[] stores, int count, WorkloadSettings settings, CancellationToken cancellationToken)
    {
        var batches = Enumerable.Range(0, (count + settings.BatchSize - 1) / settings.BatchSize).SelectMany(batch => Enumerable.Range(0, stores.Length).Select(store => (Store: store, Offset: batch * settings.BatchSize)));
        return Parallel.ForEachAsync(batches, new ParallelOptions { MaxDegreeOfParallelism = settings.Appenders, CancellationToken = cancellationToken }, async (batch, token) =>
        {
            var length = Math.Min(settings.BatchSize, count - batch.Offset);
            var events = Enumerable.Range(batch.Offset, length).Select(i => (object)new BenchmarkEvent($"event-{i}", i, DateTimeOffset.UtcNow)).ToArray();
            var result = await stores[batch.Store].EventLog.AppendMany(EventSourceId.New(), events).WaitAsync(token);
            if (!result.IsSuccess)
            {
                throw new HarnessFailure("AppendMany rejected a workload batch.");
            }
        });
    }

    static async Task WaitForObservers(IEventStore[] stores, int expected, CancellationToken cancellationToken)
    {
        while (stores.Any(store => ProfileReactor.Count(store.Name.ToString(), store.Namespace.ToString()) < expected))
        {
            await Task.Delay(100, cancellationToken);
        }
    }
}
