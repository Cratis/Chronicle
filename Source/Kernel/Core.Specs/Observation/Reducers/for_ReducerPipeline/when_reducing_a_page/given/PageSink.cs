// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.given;

/// <summary>
/// A sink that records how a page is read and written: prefetches, single reads, writes and bulk windows.
/// </summary>
public class PageSink : ISink, ISinkPagePrefetch
{
    /// <summary>
    /// Gets the states the sink holds, by partition.
    /// </summary>
    public Dictionary<string, ExpandoObject> States { get; } = [];

    /// <summary>
    /// Gets the keys of every prefetch, in call order.
    /// </summary>
    public List<Key[]> Prefetches { get; } = [];

    /// <summary>
    /// Gets the partitions written, in call order.
    /// </summary>
    public List<string> Writes { get; } = [];

    /// <summary>
    /// Gets the calls in the order they were made.
    /// </summary>
    public List<string> Calls { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a bulk window is already open on the sink.
    /// </summary>
    public bool IsInBulkMode { get; set; }

    /// <summary>
    /// Gets or sets the partitions whose writes fail.
    /// </summary>
    public HashSet<string> FailingWrites { get; set; } = [];

    /// <summary>
    /// Gets or sets the failures the end of the bulk window reports.
    /// </summary>
    public IEnumerable<FailedPartition> FailuresAtEndOfBulk { get; set; } = [];

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public Task PrefetchPage(IEnumerable<Key> keys)
    {
        Calls.Add(nameof(PrefetchPage));
        Prefetches.Add([.. keys]);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void ClearPrefetchPage() => Calls.Add(nameof(ClearPrefetchPage));

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        Calls.Add($"{nameof(FindOrDefault)}:{key.Value}");
        return Task.FromResult<ExpandoObject?>(States.TryGetValue(key.Value.ToString()!, out var state) ? state : null);
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindInPrimaryOrDefault(Key key) => FindOrDefault(key);

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) =>
        Task.FromResult(Option<Key>.None());

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        var partition = key.Value.ToString()!;
        Calls.Add($"{nameof(ApplyChanges)}:{partition}");
        Writes.Add(partition);
        return Task.FromResult<IEnumerable<FailedPartition>>(FailingWrites.Contains(partition) ? [new FailedPartition(key, eventSequenceNumber) { Reason = "write failed" }] : []);
    }

    /// <inheritdoc/>
    public Task BeginBulk()
    {
        Calls.Add(nameof(BeginBulk));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk()
    {
        Calls.Add(nameof(EndBulk));
        return Task.FromResult(FailuresAtEndOfBulk);
    }

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Task.FromResult(new ReadModelInstances([], 0));

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Observable.Empty<IEnumerable<ExpandoObject>>();
}
