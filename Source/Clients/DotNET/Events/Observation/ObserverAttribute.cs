// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Observation;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an observer.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ObserverAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for an observer.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the unique identifier for an event log.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = EventSequenceId.Log;

    /// <summary>
    /// Initializes a new instance of <see cref="ObserverAttribute"/>.
    /// </summary>
    /// <param name="observerIdAsString">Unique identifier as string.</param>
    /// <param name="inbox">Whether or not to observe inbox. If false, it will observe the default event log.</param>
    public ObserverAttribute(string observerIdAsString, bool inbox = false)
    {
        ObserverId = observerIdAsString;
        EventSequenceId = inbox ? EventSequenceId.Inbox : EventSequenceId.Log;
    }
}
