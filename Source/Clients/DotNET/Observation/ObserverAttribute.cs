// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;

namespace Cratis.Observation;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an observer.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ObserverAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObserverAttribute"/>.
    /// </summary>
    /// <param name="observerIdAsString"><see cref="ObserverId"/> represented as string. Must be a valid Guid.</param>
    /// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
    public ObserverAttribute(string observerIdAsString, string? eventSequence = default)
    {
        ObserverId = observerIdAsString;
        EventSequenceId = eventSequence ?? EventSequenceId.Log;
    }

    /// <summary>
    /// Gets the unique identifier for an observer.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the unique identifier for an event log.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = EventSequenceId.Log;
}
