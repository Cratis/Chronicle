// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> that does nothing.
/// </summary>
public class NullSink : ISink
{
    /// <summary>
    /// Gets a singleton instance of <see cref="NullSink"/>.
    /// </summary>
    public static readonly NullSink Instance = new();

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Null;

    /// <inheritdoc/>
    public SinkTypeName Name => "Null sink";

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        Task.FromResult<IEnumerable<FailedPartition>>([]);

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
    public Task<ExpandoObject?> FindOrDefault(Key key) => Task.FromResult<ExpandoObject?>(null);

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue) => Task.FromResult(Option<Key>.None());

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;
}
