// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Recovers durable queues independently of event-log data and silo-local activations.
/// </summary>
public static class OccurrenceRecovery
{
    static readonly TimeSpan _scopeTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Wakes triggers with unfinished work in one scope, with bounded fan-out and isolated failures.
    /// </summary>
    /// <param name="factory">The grain factory.</param>
    /// <param name="storage">The scoped occurrence store.</param>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace or null for application scope.</param>
    /// <param name="logger">The caller's logger.</param>
    /// <param name="cancellationToken">Token bounding discovery and wake-up work.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Rehydrate(IGrainFactory factory, IOccurrencesStorage storage, EventStoreName eventStore, string? @namespace, ILogger logger, CancellationToken cancellationToken = default)
    {
        var triggers = await storage.GetPendingTriggers().WaitAsync(cancellationToken);
        await Parallel.ForEachAsync(triggers, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken }, async (trigger, token) =>
        {
            try
            {
                await OccurrenceQueueKey.Resolve(factory, eventStore, @namespace, trigger.Value).Wake().WaitAsync(token);
            }
            catch (Exception exception) when (!token.IsCancellationRequested)
            {
                logger.FailedWakingOccurrenceQueue(exception, eventStore, @namespace, trigger.Value);
            }
        });
    }

    /// <summary>
    /// Expires one connection's leases in every scope without letting one failure stop the others.
    /// </summary>
    /// <param name="storage">The kernel storage.</param>
    /// <param name="worker">The disconnected connection identity.</param>
    /// <param name="now">The kernel instant.</param>
    /// <param name="logger">The caller's logger.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ExpireLeases(IStorage storage, string worker, DateTimeOffset now, ILogger logger)
    {
        try
        {
            foreach (var name in await storage.GetEventStores().WaitAsync(_scopeTimeout))
            {
                try
                {
                    var store = storage.GetEventStore(name);
                    await ExpireScope(() => store.Occurrences, name, null);
                    foreach (var state in await store.Namespaces.GetAll().WaitAsync(_scopeTimeout))
                    {
                        await ExpireScope(() => store.GetNamespace(state.Name).Occurrences, name, state.Name.Value);
                    }
                }
                catch (Exception exception)
                {
                    logger.FailedDiscoveringOccurrenceScopes(exception, worker, name.Value);
                }
            }
        }
        catch (Exception exception)
        {
            // This runs outside the disconnect turn; no background failure may go unobserved.
            logger.FailedDiscoveringOccurrenceScopes(exception, worker, null);
        }

        async Task ExpireScope(Func<IOccurrencesStorage> scope, EventStoreName eventStore, string? @namespace)
        {
            try
            {
                await scope().ExpireLeases(worker, now).WaitAsync(_scopeTimeout);
            }
            catch (Exception exception)
            {
                logger.FailedExpiringOccurrenceLeases(exception, worker, eventStore, @namespace);
            }
        }
    }
}
