// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines storage for tracking closed event streams.
/// </summary>
public interface IClosedStreamsConstraintStorage
{
    /// <summary>
    /// Checks whether a stream is closed.
    /// </summary>
    /// <param name="streamType">The <see cref="EventStreamType"/> of the stream.</param>
    /// <param name="streamId">The <see cref="EventStreamId"/> of the stream.</param>
    /// <returns><see langword="true"/> if the stream is closed; otherwise <see langword="false"/>.</returns>
    Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId);

    /// <summary>
    /// Marks a stream as closed.
    /// </summary>
    /// <param name="streamType">The <see cref="EventStreamType"/> of the stream.</param>
    /// <param name="streamId">The <see cref="EventStreamId"/> of the stream.</param>
    /// <returns>Awaitable task.</returns>
    Task CloseStream(EventStreamType streamType, EventStreamId streamId);
}
