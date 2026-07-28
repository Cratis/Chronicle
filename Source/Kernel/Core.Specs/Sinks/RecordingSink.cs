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
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// An <see cref="ISink"/> that records the <see cref="SinkWriteMode"/> every write is requested with.
/// </summary>
/// <remarks>
/// Hand written rather than mocked so the recording is of the overload the pipeline actually calls, not of the
/// default interface implementation a proxy may or may not intercept.
/// </remarks>
public class RecordingSink : ISink
{
    readonly List<SinkWriteMode> _writeModes = [];

    /// <summary>
    /// Gets the write modes recorded, in call order.
    /// </summary>
    public IReadOnlyList<SinkWriteMode> WriteModes => _writeModes;

    /// <summary>
    /// Gets or sets the instance <see cref="FindOrDefault"/> answers with.
    /// </summary>
    public ExpandoObject? Existing { get; set; }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key) => Task.FromResult(Existing);

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) =>
        Task.FromResult(Option<Key>.None());

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        _writeModes.Add(mode);
        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Task.CompletedTask;

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
