// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the context for an observer subscriber.
/// </summary>
/// <param name="Metadata">Optional metadata associated.</param>
public record ObserverSubscriberContext(object? Metadata)
{
    /// <summary>
    /// Gets the deadline after which the caller no longer accepts a result.
    /// </summary>
    public DateTimeOffset? DeliverBy { get; init; }

    /// <summary>
    /// Gets the deadline after which the subscriber stops holding the abandoned call open.
    /// </summary>
    public DateTimeOffset? AbandonAt { get; init; }

    /// <summary>
    /// Adds fresh deadlines for this dispatch, leaving disabled deliveries unbounded.
    /// </summary>
    /// <param name="timeout">The caller's timeout.</param>
    /// <param name="hold">The abandoned delivery hold.</param>
    /// <param name="clock">The clock.</param>
    /// <returns>The context for this dispatch.</returns>
    public ObserverSubscriberContext WithDeadlines(TimeSpan timeout, TimeSpan hold, TimeProvider clock) =>
        timeout > TimeSpan.Zero && hold > TimeSpan.Zero
            ? this with { DeliverBy = clock.GetUtcNow() + timeout, AbandonAt = clock.GetUtcNow() + timeout + hold }
            : this with { DeliverBy = null, AbandonAt = null };
}
