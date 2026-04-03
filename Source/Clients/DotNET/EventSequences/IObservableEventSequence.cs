// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the ability to subscribe to non-transactional event appended notifications on an event sequence.
/// </summary>
/// <remarks>
/// <see cref="EventSequence"/> (and thus <see cref="EventLog"/>) implement both this interface and
/// <see cref="IEventSequence"/>. Consumers that only need observability should depend on this interface;
/// consumers that need to append should depend on <see cref="IEventSequence"/>.
/// Check for this interface at runtime to subscribe: <c>if (eventLog is IObservableEventSequence obs) obs.Subscribe(...);</c>.
/// </remarks>
public interface IObservableEventSequence
{
    /// <summary>
    /// Subscribe to receive notifications when events are appended directly (non-transactionally) to this event sequence.
    /// </summary>
    /// <param name="observer">The <see cref="IObserveEventAppended"/> to subscribe.</param>
    /// <returns>This event sequence, for method chaining.</returns>
    IObservableEventSequence Subscribe(IObserveEventAppended observer);

    /// <summary>
    /// Unsubscribe from event appended notifications.
    /// </summary>
    /// <param name="observer">The <see cref="IObserveEventAppended"/> to unsubscribe.</param>
    /// <returns>This event sequence, for method chaining.</returns>
    IObservableEventSequence Unsubscribe(IObserveEventAppended observer);
}
