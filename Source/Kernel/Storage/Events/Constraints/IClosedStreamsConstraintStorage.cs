// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines storage for tracking owned closed event stream scopes.
/// </summary>
public interface IClosedStreamsConstraintStorage
{
    /// <summary>
    /// Find closures covering a target in one round trip.
    /// </summary>
    /// <param name="target">The target scope.</param>
    /// <param name="masks">The closure masks to check.</param>
    /// <returns>The covering closures.</returns>
    Task<IEnumerable<ClosedStream>> GetCovering(ClosedStreamScope target, IEnumerable<ClosedStreamDimensions> masks);

    /// <summary>
    /// Get all masks currently persisted.
    /// </summary>
    /// <returns>The distinct masks.</returns>
    Task<IEnumerable<ClosedStreamDimensions>> GetDimensionsInUse();

    /// <summary>
    /// Upsert a closure by owner and normalized scope.
    /// </summary>
    /// <param name="closure">The closure to persist.</param>
    /// <returns>Awaitable task.</returns>
    Task Close(ClosedStream closure);

    /// <summary>
    /// Remove only the exact scope belonging to an owner.
    /// </summary>
    /// <param name="owner">The owning constraint or manual owner.</param>
    /// <param name="scope">The exact scope.</param>
    /// <returns>True if a closure was removed.</returns>
    Task<bool> Reopen(ClosedStreamOwner owner, ClosedStreamScope scope);

    /// <summary>
    /// Get closures belonging to an owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>The owner's closures.</returns>
    Task<IEnumerable<ClosedStream>> GetForOwner(ClosedStreamOwner owner);

    /// <summary>
    /// Remove every closure belonging to an owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllFor(ClosedStreamOwner owner);

    /// <summary>
    /// List closures within an optional scope, with paging.
    /// </summary>
    /// <param name="within">Only return closures covered by this scope, or all closures if null.</param>
    /// <param name="skip">The number of rows to skip.</param>
    /// <param name="take">The maximum number of rows, or null for all.</param>
    /// <returns>The matching closures.</returns>
    Task<IEnumerable<ClosedStream>> GetAll(ClosedStreamScope? within = default, int skip = 0, int? take = default);

    /// <summary>
    /// Checks whether a stream is covered by a closure.
    /// </summary>
    /// <param name="streamType">The stream type.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>True if the stream is closed.</returns>
    [Obsolete("Use GetCovering with a ClosedStreamScope instead.")]
    async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId) =>
        (await GetCovering(new(EventStreamType: streamType, EventStreamId: streamId), await GetDimensionsInUse())).Any();

    /// <summary>
    /// Marks a stream as manually closed.
    /// </summary>
    /// <param name="streamType">The stream type.</param>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>Awaitable task.</returns>
    [Obsolete("Use Close with a ClosedStream instead.")]
    Task CloseStream(EventStreamType streamType, EventStreamId streamId) =>
        Close(new(new(EventStreamType: streamType, EventStreamId: streamId), ClosedStreamOwner.Manual, EventSequenceNumber.Unavailable, null));
}
