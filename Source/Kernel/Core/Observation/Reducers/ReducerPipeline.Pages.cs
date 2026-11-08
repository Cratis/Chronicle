// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Partial for reducing a page of partitions in one delivery.
/// </summary>
public partial class ReducerPipeline
{
    static readonly IReadOnlyDictionary<Key, ReducerSubscriberResult> _noResults = new Dictionary<Key, ReducerSubscriberResult>();

    /// <inheritdoc/>
    public async Task<ReducerPageOutcome> ReducePage(IReadOnlyList<ReducerContext> contexts, ReducerPageDelegate reducer, bool createNotification = true)
    {
        if (contexts.Count == 0)
        {
            return new(_noResults, new Dictionary<Key, ReadModelChangeNotification>(), []);
        }

        var keys = contexts.Select(_ => _.Key).ToArray();
        using var memoryTicket = memoryGate is null ? ReducerMemoryTicket.None : await memoryGate.EnterPage(_memoryScope, keys);

        // An open window belongs to whoever opened it - a replay holds one for its whole walk - and beginning another would
        // replace it. The page's writes join an open window, and the page opens its own only when none is open.
        var ownsWindow = !Sink.IsInBulkMode;
        if (ownsWindow)
        {
            await Sink.BeginBulk();
        }

        var failures = new List<FailedPartition>();
        var notifications = new Dictionary<Key, ReadModelChangeNotification>();
        IReadOnlyDictionary<Key, ReducerSubscriberResult> results;
        try
        {
            var stored = await ReadPage(contexts, keys);
            if (memoryGate is not null)
            {
                await memoryGate.ObservePage(memoryTicket, _memoryScope, contexts.Select((context, index) => (context.Key, stored[index].Stored)));
            }

            results = await reducer([.. contexts.Select((context, index) => new ReducerPagePartition(context.Key, context.Events, stored[index].Released))]);
            for (var index = 0; index < contexts.Count; index++)
            {
                var context = contexts[index];

                // A partition the client failed, deferred or did not answer for writes nothing, so it is delivered again from its stored state.
                if (!results.TryGetValue(context.Key, out var result) || result.ObserverResult.State != ObserverSubscriberState.Ok)
                {
                    continue;
                }

                var (notification, writeFailures) = await WritePartition(context, stored[index], result, createNotification);
                failures.AddRange(writeFailures);
                if (notification is not null)
                {
                    notifications[context.Key] = notification;
                }
            }
        }
        finally
        {
            (Sink as ISinkPagePrefetch)?.ClearPrefetchPage();
            if (ownsWindow)
            {
                failures.AddRange(await Sink.EndBulk());
            }
        }

        // A change is only announced once it is written, and a partition whose write failed has none to announce.
        var announced = notifications
            .Where(notification => !failures.Exists(failure => Equals(failure.EventSourceId.Value, notification.Key.Value)))
            .ToDictionary();

        return new(results, announced, failures);
    }

    /// <summary>
    /// Reads the states of every partition in a page, in one sink read where the sink can prefetch.
    /// </summary>
    /// <param name="contexts">The <see cref="ReducerContext"/> of every partition in the page.</param>
    /// <param name="keys">The <see cref="Key"/> of every partition in the page.</param>
    /// <returns>The <see cref="StoredState"/> of each partition, in the order of <paramref name="contexts"/>.</returns>
    async Task<StoredState[]> ReadPage(IReadOnlyList<ReducerContext> contexts, IEnumerable<Key> keys)
    {
        if (Sink is ISinkPagePrefetch prefetch)
        {
            await prefetch.PrefetchPage(keys);
        }

        var stored = new StoredState[contexts.Count];
        for (var index = 0; index < contexts.Count; index++)
        {
            stored[index] = await Release(await Sink.FindOrDefault(contexts[index].Key));
        }

        return stored;
    }

    /// <summary>
    /// Writes one partition of a page, failing only that partition when its write throws.
    /// </summary>
    /// <param name="context">The <see cref="ReducerContext"/> of the partition.</param>
    /// <param name="stored">The <see cref="StoredState"/> the partition was reduced from.</param>
    /// <param name="result">The <see cref="ReducerSubscriberResult"/> the client returned.</param>
    /// <param name="createNotification">Whether to build the notification of the change.</param>
    /// <returns>The change to notify watchers of, if any, and the partitions the sink reported as failed.</returns>
    async Task<(ReadModelChangeNotification? Notification, FailedPartition[] Failures)> WritePartition(ReducerContext context, StoredState stored, ReducerSubscriberResult result, bool createNotification)
    {
        try
        {
            return await Write(context, stored, result, createNotification);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (null, [new FailedPartition(context.Key, context.Events.First().Context.SequenceNumber) { Reason = exception.Message }]);
        }
    }
}
