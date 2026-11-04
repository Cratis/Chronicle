// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Declares an event type a batch method observes. Apply it once per event type to a method that takes an <see cref="EventBatch"/>.
/// </summary>
/// <remarks>
/// Use <see cref="ObservesAttribute{TEvent}"/> to apply it. A method that takes an <see cref="EventBatch"/> is called with
/// every event of the types it observes in a delivered page, and pattern-matches on them. Each event type an observer handles
/// reaches exactly one of its methods.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class ObservesAttribute : Attribute
{
    /// <summary>
    /// Gets the event type observed.
    /// </summary>
    public abstract Type EventType { get; }
}
