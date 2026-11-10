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
/// <remarks>
/// The sink is one shared instance per read model and namespace, and its bulk window is shared with everything writing
/// through it: a write made while a window is open is buffered, and only becomes durable when the window is flushed. A page
/// therefore never opens, flushes or closes a window. Where the sink can, it reads and writes the page's partitions without
/// one (<see cref="ISinkPages"/>), so every write is durable, or reported as failed, when the page returns. A sink without
/// that capability is written one partition at a time. A window someone else opened - a replay holds one for its whole
/// walk - is joined, exactly as a single partition's writes join it.
/// </remarks>
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

        var pages = Sink.IsInBulkMode ? null : Sink as ISinkPages;
        try
        {
            var stored = await ReadPage(contexts, keys, pages);
            if (memoryGate is not null)
            {
                await memoryGate.ObservePage(memoryTicket, _memoryScope, contexts.Select((context, index) => (context.Key, stored[index].Stored)));
            }

            var results = await reducer([.. contexts.Select((context, index) => new ReducerPagePartition(context.Key, context.Events, stored[index].Released))]);
            var (writes, failures) = await PrepareWrites(contexts, stored, results);
            failures.AddRange(pages is null ? await WriteOneByOne(writes) : await WritePage(pages, writes));

            // A change is only announced once it is written, and a partition whose write failed has none to announce.
            var notifications = createNotification
                ? writes
                    .Where(write => !failures.Exists(failure => Equals(failure.EventSourceId.Value, write.Context.Key.Value)))
                    .ToDictionary(write => write.Context.Key, write => CreateNotification(write.Context, write.Result.ReadModelState, write.Stored.Released, write.Write.StoredState))
                : new Dictionary<Key, ReadModelChangeNotification>();

            return new(results, notifications, failures);
        }
        finally
        {
            if (pages is null)
            {
                (Sink as ISinkPagePrefetch)?.ClearPrefetchPage();
            }
        }
    }

    /// <summary>
    /// Writes the partitions of a page through the sink's page capability.
    /// </summary>
    /// <param name="pages">The sink's <see cref="ISinkPages"/>.</param>
    /// <param name="writes">The <see cref="PageWrite"/> of each partition to write.</param>
    /// <returns>The partitions the sink reported as failed.</returns>
    /// <remarks>
    /// A sink that cannot tell how its page write went has written some, all or none of it, so every partition of the page
    /// is reported as failed with an unknown outcome and is delivered again.
    /// </remarks>
    static async Task<IEnumerable<FailedPartition>> WritePage(ISinkPages pages, IReadOnlyList<PageWrite> writes)
    {
        if (writes.Count == 0)
        {
            return [];
        }

        try
        {
            return await pages.ApplyPage([.. writes.Select(write => write.Write.AsPageWrite)]);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return [.. writes.Select(write => FailedWrite(write.Context, exception) with { OutcomeUnknown = true })];
        }
    }

    static FailedPartition FailedWrite(ReducerContext context, Exception exception) =>
        new(context.Key, context.Events.First().Context.SequenceNumber) { Reason = exception.Message };

    /// <summary>
    /// Reads the states of every partition in a page, in as few sink reads as the sink can.
    /// </summary>
    /// <param name="contexts">The <see cref="ReducerContext"/> of every partition in the page.</param>
    /// <param name="keys">The <see cref="Key"/> of every partition in the page.</param>
    /// <param name="pages">The sink's <see cref="ISinkPages"/>, or null when the page is not read and written through it.</param>
    /// <returns>The <see cref="StoredState"/> of each partition, in the order of <paramref name="contexts"/>.</returns>
    async Task<StoredState[]> ReadPage(IReadOnlyList<ReducerContext> contexts, Key[] keys, ISinkPages? pages)
    {
        var stored = new StoredState[contexts.Count];
        if (pages is not null)
        {
            var found = await pages.FindPage(keys);
            for (var index = 0; index < contexts.Count; index++)
            {
                stored[index] = await Release(found[index]);
            }

            return stored;
        }

        // Prefetching fills the open window the page joins, so its states are read in one sink read there.
        if (Sink is ISinkPagePrefetch prefetch)
        {
            await prefetch.PrefetchPage(keys);
        }

        for (var index = 0; index < contexts.Count; index++)
        {
            stored[index] = await Release(await Sink.FindOrDefault(contexts[index].Key));
        }

        return stored;
    }

    /// <summary>
    /// Works out what to write for every partition the client reduced, failing only a partition whose state cannot be prepared.
    /// </summary>
    /// <param name="contexts">The <see cref="ReducerContext"/> of every partition in the page.</param>
    /// <param name="stored">The <see cref="StoredState"/> of each partition, in the order of <paramref name="contexts"/>.</param>
    /// <param name="results">The <see cref="ReducerSubscriberResult"/> the client returned for each partition it answered for.</param>
    /// <returns>The writes, in page order, and the partitions that failed while being prepared.</returns>
    async Task<(List<PageWrite> Writes, List<FailedPartition> Failures)> PrepareWrites(
        IReadOnlyList<ReducerContext> contexts,
        StoredState[] stored,
        IReadOnlyDictionary<Key, ReducerSubscriberResult> results)
    {
        var writes = new List<PageWrite>();
        var failures = new List<FailedPartition>();
        for (var index = 0; index < contexts.Count; index++)
        {
            var context = contexts[index];

            // A partition the client failed, deferred or did not answer for writes nothing, so it is delivered again from its stored state.
            if (!results.TryGetValue(context.Key, out var result) || result.ObserverResult.State != ObserverSubscriberState.Ok)
            {
                continue;
            }

            try
            {
                if (await Prepare(context, stored[index], result) is { } write)
                {
                    writes.Add(new(context, stored[index], result, write));
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(FailedWrite(context, exception));
            }
        }

        return (writes, failures);
    }

    /// <summary>
    /// Writes the partitions of a page one at a time, failing only a partition whose write throws.
    /// </summary>
    /// <param name="writes">The <see cref="PageWrite"/> of each partition to write.</param>
    /// <returns>The partitions the sink reported as failed.</returns>
    async Task<IEnumerable<FailedPartition>> WriteOneByOne(IEnumerable<PageWrite> writes)
    {
        var failures = new List<FailedPartition>();
        foreach (var write in writes)
        {
            try
            {
                failures.AddRange(await Sink.ApplyChanges(write.Write.Key, write.Write.Changeset, write.Write.EventSequenceNumber, write.Write.Mode));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(FailedWrite(write.Context, exception));
            }
        }

        return failures;
    }

    /// <summary>
    /// Represents a partition of a page that has a state to write.
    /// </summary>
    /// <param name="Context">The <see cref="ReducerContext"/> of the partition.</param>
    /// <param name="Stored">The <see cref="StoredState"/> it was reduced from.</param>
    /// <param name="Result">The <see cref="ReducerSubscriberResult"/> the client returned.</param>
    /// <param name="Write">The <see cref="PreparedWrite"/> to apply.</param>
    sealed record PageWrite(ReducerContext Context, StoredState Stored, ReducerSubscriberResult Result, PreparedWrite Write);
}
