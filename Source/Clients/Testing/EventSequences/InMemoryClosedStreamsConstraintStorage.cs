// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage.Events.Constraints;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IClosedStreamsConstraintStorage"/> for testing.
/// </summary>
internal sealed class InMemoryClosedStreamsConstraintStorage : IClosedStreamsConstraintStorage
{
    readonly HashSet<(string StreamType, string StreamId)> _closedStreams = [];

    /// <inheritdoc/>
    public Task<bool> IsStreamClosed(KernelEvents::EventStreamType streamType, KernelEvents::EventStreamId streamId) =>
        Task.FromResult(_closedStreams.Contains((streamType.Value, streamId.Value)));

    /// <inheritdoc/>
    public Task CloseStream(KernelEvents::EventStreamType streamType, KernelEvents::EventStreamId streamId)
    {
        _closedStreams.Add((streamType.Value, streamId.Value));
        return Task.CompletedTask;
    }
}
