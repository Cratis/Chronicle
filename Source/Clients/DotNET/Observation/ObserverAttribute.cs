// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an observer.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ObserverAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the observer.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = EventSequenceId.Log;

    /// <summary>
    /// Initializes a new instance of <see cref="ObserverAttribute"/>.
    /// </summary>
    /// <param name="observerIdAsString"><see cref="ObserverId"/> represented as string. Must be a valid Guid.</param>
    /// <param name="inbox">Whether or not to observe inbox. If false, it will observe the default event log.</param>
    /// <param name="eventSequence">Optional the name of the event sequence to observe, this will take precedence over inbox.</param>
    public ObserverAttribute(string observerIdAsString, bool inbox = false, string? eventSequence = default)
    {
        ObserverId = observerIdAsString;
        EventSequenceId = eventSequence ?? (inbox ? EventSequenceId.Inbox : EventSequenceId.Log);
    }
}
