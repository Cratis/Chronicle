// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Partial for reading and writing a page of instances without the shared bulk window.
/// </summary>
public partial class Sink : ISinkPages
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExpandoObject?>> FindPage(IReadOnlyList<Key> keys)
    {
        if (_isBulkMode)
        {
            return await FindOneByOne(keys);
        }

        // A prefetch of its own: the sink's prefetch belongs to the open window, which this read must not touch.
        var found = new Dictionary<BsonValue, BsonDocument>();
        await new SinkPagePrefetch(converter).Prefetch(Collection, keys, (key, document) => found[SinkCacheKey.Identity(converter.ToBsonValue(key))] = document);
        var schema = readModel.GetSchemaForLatestGeneration();
        return [.. keys.Select(key => found.TryGetValue(SinkCacheKey.Identity(converter.ToBsonValue(key)), out var document)
            ? expandoObjectConverter.ToExpandoObject(document, schema)
            : null)];
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyPage(IReadOnlyList<SinkPageWrite> pageWrites)
    {
        _bulkCancellation.Token.ThrowIfCancellationRequested();
        if (_isBulkMode || writes.IsInBatch)
        {
            return await ApplyOneByOne(pageWrites);
        }

        var page = new PageOperations();
        var failures = new List<FailedPartition>();
        foreach (var write in pageWrites)
        {
            // A join reads and writes other instances, and a guarded upsert treats a rejected insert as accepted;
            // neither fits one ordered write of the page, so each is written on its own, straight away.
            if (write.Changeset.HasJoined() || write.Mode == SinkWriteMode.GuardedUpsert)
            {
                failures.AddRange(await ApplyChanges(write.Key, write.Changeset, write.EventSequenceNumber, write.Mode));
                continue;
            }

            var operations = new PageOperations();
            try
            {
                await PlanPageWrite(write, operations);
                page.Append(operations);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures.Add(new FailedPartition(PartitionFor(write.Key, write.Changeset), write.EventSequenceNumber) { Reason = exception.Message });
            }
        }

        failures.AddRange(await WritePage(page));
        return failures;
    }

    async Task<IReadOnlyList<ExpandoObject?>> FindOneByOne(IReadOnlyList<Key> keys)
    {
        var states = new ExpandoObject?[keys.Count];
        for (var index = 0; index < keys.Count; index++)
        {
            states[index] = await FindOrDefault(keys[index]);
        }

        return states;
    }

    async Task<IEnumerable<FailedPartition>> ApplyOneByOne(IReadOnlyList<SinkPageWrite> pageWrites)
    {
        var failures = new List<FailedPartition>();
        foreach (var write in pageWrites)
        {
            failures.AddRange(await ApplyChanges(write.Key, write.Changeset, write.EventSequenceNumber, write.Mode));
        }

        return failures;
    }

    /// <summary>
    /// Plans the operations of one write of a page exactly as a direct write plans them, collecting them instead of running them.
    /// </summary>
    /// <param name="write">The <see cref="SinkPageWrite"/> to plan.</param>
    /// <param name="operations">The <see cref="PageOperations"/> to collect the operations in.</param>
    /// <returns>Awaitable task.</returns>
    async Task PlanPageWrite(SinkPageWrite write, PageOperations operations)
    {
        var (key, changeset, eventSequenceNumber, mode) = write;
        var options = new SinkWriteOptions(mode, Partition: PartitionFor(key, changeset), Page: operations);
        var plan = PlanWrite(key, changeset);
        if (changeset.HasBeenRemoved())
        {
            await ApplyRemoval(key, eventSequenceNumber, plan.Filter, options);
            return;
        }

        if (GuardWithWatermark(plan, key, mode, eventSequenceNumber) is { } guardedPlan)
        {
            await WriteAfterJoin(key, changeset, eventSequenceNumber, guardedPlan, options);
        }
    }

    /// <summary>
    /// Writes the operations of a page in one ordered bulk write, reporting the partitions whose writes failed.
    /// </summary>
    /// <param name="page">The <see cref="PageOperations"/> to write.</param>
    /// <returns>The partitions whose writes failed, or whose outcome is unknown.</returns>
    async Task<IEnumerable<FailedPartition>> WritePage(PageOperations page)
    {
        if (page.Operations.Count == 0)
        {
            return [];
        }

        var batch = new BulkBatch(page.Operations, page.Metadata, [], [], new HashSet<WriteModel<BsonDocument>>());
        try
        {
            return await WriteBatch(batch, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return UnknownBatchOutcome(batch, exception).Failures;
        }
    }

    /// <summary>
    /// Collects the operations of a page, in order, with the partition and event sequence number of each.
    /// </summary>
    sealed class PageOperations
    {
        /// <summary>
        /// Gets the operations, in the order to write them.
        /// </summary>
        public List<WriteModel<BsonDocument>> Operations { get; } = [];

        /// <summary>
        /// Gets the instance, partition and event sequence number of each operation, by its index.
        /// </summary>
        public Dictionary<int, (Key ReadModelKey, Key EventSourceId, EventSequenceNumber SequenceNumber)> Metadata { get; } = [];

        /// <summary>
        /// Adds an operation.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <param name="key">The <see cref="Key"/> of the instance it writes.</param>
        /// <param name="partition">The partition it belongs to.</param>
        /// <param name="eventSequenceNumber">The sequence number of the event that caused it.</param>
        public void Add(WriteModel<BsonDocument> operation, Key key, Key partition, EventSequenceNumber eventSequenceNumber)
        {
            Metadata[Operations.Count] = (key, partition, eventSequenceNumber);
            Operations.Add(operation);
        }

        /// <summary>
        /// Appends every operation of another collection.
        /// </summary>
        /// <param name="other">The <see cref="PageOperations"/> to append.</param>
        public void Append(PageOperations other)
        {
            for (var index = 0; index < other.Operations.Count; index++)
            {
                var (key, partition, eventSequenceNumber) = other.Metadata[index];
                Add(other.Operations[index], key, partition, eventSequenceNumber);
            }
        }
    }
}
