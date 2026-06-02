// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The errors that can occur when completing a stream on an <see cref="IEventSequence"/>.
/// </summary>
public enum CompleteStreamError
{
    /// <summary>
    /// The stream was already completed previously.
    /// </summary>
    /// <remarks>
    /// Completing a stream is idempotent at the storage level — the stream remains completed regardless of how many
    /// times completion is requested — but the caller is informed via this error so it can distinguish a freshly
    /// completed stream from one that was already in the completed state.
    /// </remarks>
    AlreadyCompleted = 0,

    /// <summary>
    /// The default stream (<see cref="EventStreamType.All"/> paired with the default <see cref="EventStreamId"/>) cannot be completed.
    /// </summary>
    /// <remarks>
    /// The default stream is the catch-all stream events fall into when no specific stream type or identifier is supplied.
    /// Completing it would block all future appends to the event sequence and is therefore not permitted.
    /// </remarks>
    DefaultStreamCannotBeCompleted = 1,
}
