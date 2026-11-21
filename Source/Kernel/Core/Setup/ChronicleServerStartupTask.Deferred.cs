// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;

namespace Orleans.Hosting;

/// <summary>
/// The part of <see cref="ChronicleServerStartupTask"/> that runs after the silo is ready.
/// </summary>
/// <remarks>
/// <para>
/// Everything here warms grains - rehydrates observers, jobs, event sequences, subscriptions, webhooks, captures and
/// registers projection definitions - and calls grains that may be hosted on other silos. During a rolling restart the
/// silo that is still serving every client answers slowly, so waiting for this work held the restarted silo out of the
/// cluster for as long as the other one stayed busy.
/// </para>
/// <para>
/// None of it decides whether a request is served correctly. A grain activates on first use, reads its own persisted state and
/// repairs itself, observers are also driven by their reminders and by client registrations, and the engine compiles a
/// projection from storage the first time it is needed. Deferring the work therefore delays warming, not correctness.
/// Every operation is idempotent, so two silos doing it at once, or a pass repeated after a failure, is harmless.
/// </para>
/// </remarks>
internal sealed partial class ChronicleServerStartupTask
{
    /// <summary>
    /// How many passes over the work that has not completed are made before giving up until the next restart.
    /// </summary>
    const int MaxDeferredPasses = 6;

    /// <summary>
    /// How many namespaces of one event store are rehydrated at once.
    /// </summary>
    const int MaxConcurrentDeferredNamespaces = 4;

    static readonly TimeSpan _initialDeferredDelay = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _maxDeferredDelay = TimeSpan.FromMinutes(2);

    readonly Lock _deferredLock = new();
    CancellationTokenSource? _deferredCancellation;
    Task _deferredWork = Task.CompletedTask;

    /// <summary>
    /// Gets the task representing the deferred work, for waiting on it.
    /// </summary>
    internal Task DeferredWork => _deferredWork;

    static TimeSpan DeferredDelay(int pass)
    {
        var delay = _initialDeferredDelay;
        for (var i = 1; i < pass && delay < _maxDeferredDelay; i++)
        {
            delay += delay;
        }

        return delay < _maxDeferredDelay ? delay : _maxDeferredDelay;
    }

    void StartDeferredWork(IReadOnlyList<EventStoreName> eventStores)
    {
        lock (_deferredLock)
        {
            // A second start - the same silo activating its lifecycle twice - must not run the work twice.
            if (_deferredCancellation is not null)
            {
                return;
            }

            _deferredCancellation = applicationLifetime is null
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);

            var token = _deferredCancellation.Token;
            _deferredWork = Task.Run(() => RunDeferredWork(eventStores, token), CancellationToken.None);
        }
    }

    async Task StopDeferredWork(CancellationToken cancellationToken)
    {
        CancellationTokenSource? cancellation;
        Task work;
        lock (_deferredLock)
        {
            cancellation = _deferredCancellation;
            work = _deferredWork;
            _deferredCancellation = null;
        }

        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync();
        try
        {
            // Bounded by the silo's own shutdown token: shutdown is never held up by this work.
            await work.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Either the work observed the cancellation, or shutdown stopped waiting for it.
        }

        cancellation.Dispose();
    }

    async Task RunDeferredWork(IReadOnlyList<EventStoreName> eventStores, CancellationToken token)
    {
        var started = _timeProvider.GetTimestamp();
        logger.DeferredStartupWorkStarted(eventStores.Count);
        var stores = eventStores.Select(_ => new DeferredEventStore(_)).ToList();
        var units = 0;
        var failed = 0;
        var passes = 0;
        try
        {
            for (var pass = 1; pass <= MaxDeferredPasses; pass++)
            {
                passes = pass;
                var delay = DeferredDelay(pass);
                failed = 0;
                units = 0;
                foreach (var store in stores)
                {
                    token.ThrowIfCancellationRequested();
                    if (store.Complete)
                    {
                        units += store.Namespaces;
                        continue;
                    }

                    failed += await RunDeferredEventStore(store, pass, delay, token);
                    units += store.Namespaces;
                }

                if (failed == 0)
                {
                    logger.DeferredStartupWorkFinished(_timeProvider.GetElapsedTime(started), units, 0, pass);
                    return;
                }

                if (pass < MaxDeferredPasses)
                {
                    await Task.Delay(delay, _timeProvider, token);
                }
            }

            logger.DeferredStartupWorkFinished(_timeProvider.GetElapsedTime(started), units, failed, passes);
            logger.DeferredStartupWorkGaveUp(failed, passes);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.DeferredStartupWorkStopped(_timeProvider.GetElapsedTime(started));
        }
        catch (Exception exception)
        {
            // Nothing in the background may take the silo down; the next restart repeats the work.
            logger.DeferredStartupWorkAborted(exception);
        }
    }

    async Task<int> RunDeferredEventStore(DeferredEventStore store, int pass, TimeSpan delay, CancellationToken token)
    {
        var started = _timeProvider.GetTimestamp();
        var eventStore = store.Name;

        if (store.Remaining is null)
        {
            try
            {
                await WarmEventStore(eventStore, token);
                store.Remaining = (await grainFactory.GetGrain<INamespaces>(eventStore).GetAll().WaitAsync(token)).ToList();
                store.Namespaces = store.Remaining.Count;
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                logger.DeferredStartupWorkUnitFailed(exception, eventStore, "*", delay);
                return 1;
            }
        }

        var failedNamespaces = new System.Collections.Concurrent.ConcurrentBag<EventStoreNamespaceName>();
        await Parallel.ForEachAsync(
            store.Remaining,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentDeferredNamespaces, CancellationToken = token },
            async (namespaceName, namespaceToken) =>
            {
                try
                {
                    await RehydrateNamespace(eventStore, namespaceName, namespaceToken);
                }
                catch (Exception exception) when (!namespaceToken.IsCancellationRequested)
                {
                    logger.DeferredStartupWorkUnitFailed(exception, eventStore, namespaceName.Value, delay);
                    failedNamespaces.Add(namespaceName);
                }
            });

        store.Remaining = [.. failedNamespaces];
        logger.DeferredStartupWorkEventStoreCompleted(eventStore, _timeProvider.GetElapsedTime(started), store.Namespaces, store.Remaining.Count, pass);
        return store.Remaining.Count;
    }

    async Task WarmEventStore(EventStoreName eventStore, CancellationToken token)
    {
        try
        {
            await grainFactory.GetGrain<ILifecycleOccurrences>(eventStore.Value).Ensure().WaitAsync(token);
        }
        catch (Exception exception) when (!token.IsCancellationRequested)
        {
            logger.FailedReconcilingLifecycleOccurrences(exception, eventStore.Value);
        }

        await RehydrateOccurrences(eventStore, null, token);
        await grainFactory.GetGrain<INamespaces>(eventStore).EnsureDefault().WaitAsync(token);
        await grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventStore).Ensure().WaitAsync(token);
        await grainFactory.GetGrain<IReadModelsManager>(eventStore).Ensure().WaitAsync(token);
        await grainFactory.GetGrain<IWebhooks>(eventStore).Ensure().WaitAsync(token);
        await grainFactory.GetGrain<ICapturesManager>(eventStore).Ensure().WaitAsync(token);

        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
        await projectionsManager.Ensure().WaitAsync(token);
        var projectionDefinitions = await projectionsManager.GetProjectionDefinitions().WaitAsync(token);
        await RegisterPersistedProjectionDefinitions(eventStore, projectionDefinitions).WaitAsync(token);
    }

    sealed class DeferredEventStore(EventStoreName name)
    {
        public EventStoreName Name { get; } = name;

        public List<EventStoreNamespaceName>? Remaining { get; set; }

        public int Namespaces { get; set; }

        public bool Complete => Remaining is { Count: 0 };
    }
}
