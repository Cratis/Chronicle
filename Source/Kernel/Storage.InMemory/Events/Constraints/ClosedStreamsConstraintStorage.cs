// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IClosedStreamsConstraintStorage"/>.
/// </summary>
public class ClosedStreamsConstraintStorage : IClosedStreamsConstraintStorage
{
    readonly ConcurrentDictionary<(string StreamType, string StreamId), bool> _closedStreams = [];

    /// <inheritdoc/>
    public Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId) =>
        Task.FromResult(_closedStreams.ContainsKey((streamType.Value, streamId.Value)));

    /// <inheritdoc/>
    public Task CloseStream(EventStreamType streamType, EventStreamId streamId)
    {
        _closedStreams[(streamType.Value, streamId.Value)] = true;
        return Task.CompletedTask;
    }
}
