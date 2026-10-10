// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IClosedStreamsConstraintStorage"/>.
/// </summary>
public class ClosedStreamsConstraintStorage : IClosedStreamsConstraintStorage
{
    readonly ConcurrentDictionary<(ClosedStreamOwner Owner, ClosedStreamScope Scope), ClosedStream> _closedStreams = [];

    /// <inheritdoc/>
    public Task<IEnumerable<ClosedStream>> GetCovering(ClosedStreamScope target, IEnumerable<ClosedStreamDimensions> masks)
    {
        var dimensions = masks.ToHashSet();

        return Task.FromResult(_closedStreams.Values.Where(row => dimensions.Contains(row.Scope.Dimensions) && row.Scope.Covers(target)).ToArray().AsEnumerable());
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ClosedStreamDimensions>> GetDimensionsInUse() =>
        Task.FromResult(_closedStreams.Values.Select(row => row.Scope.Dimensions).Distinct().ToArray().AsEnumerable());

    /// <inheritdoc/>
    public Task Close(ClosedStream closure)
    {
        var normalized = closure with { Scope = closure.Scope.Normalized() };
        _closedStreams[(normalized.Owner, normalized.Scope)] = normalized;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> Reopen(ClosedStreamOwner owner, ClosedStreamScope scope) =>
        Task.FromResult(_closedStreams.TryRemove((owner, scope.Normalized()), out _));

    /// <inheritdoc/>
    public Task<IEnumerable<ClosedStream>> GetForOwner(ClosedStreamOwner owner) =>
        Task.FromResult(_closedStreams.Values.Where(row => row.Owner == owner).ToArray().AsEnumerable());

    /// <inheritdoc/>
    public Task RemoveAllFor(ClosedStreamOwner owner)
    {
        foreach (var key in _closedStreams.Keys.Where(key => key.Owner == owner))
        {
            _closedStreams.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ClosedStream>> GetAll(ClosedStreamScope? within = default, int skip = 0, int? take = default)
    {
        var rows = _closedStreams.Values.Where(row => within?.Covers(row.Scope) ?? true)
            .OrderBy(row => row.Owner.Value, StringComparer.Ordinal)
            .ThenBy(row => row.Scope.EventSourceId?.Value, StringComparer.Ordinal)
            .ThenBy(row => row.Scope.EventSourceType?.Value, StringComparer.Ordinal)
            .ThenBy(row => row.Scope.EventStreamType?.Value, StringComparer.Ordinal)
            .ThenBy(row => row.Scope.EventStreamId?.Value, StringComparer.Ordinal)
            .Skip(skip);

        return Task.FromResult((take is null ? rows : rows.Take(take.Value)).ToArray().AsEnumerable());
    }

    /// <inheritdoc/>
    public async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId) =>
        (await GetCovering(new(EventStreamType: streamType, EventStreamId: streamId), await GetDimensionsInUse())).Any();

    /// <inheritdoc/>
    public Task CloseStream(EventStreamType streamType, EventStreamId streamId) =>
        Close(new(new(EventStreamType: streamType, EventStreamId: streamId), ClosedStreamOwner.Manual, EventSequenceNumber.Unavailable, null));
}
