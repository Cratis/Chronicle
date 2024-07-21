// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ObserverAttribute"/>.
/// </remarks>
/// <param name="id">Optional <see cref="Id"/> represented as string, if not used it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ObserverAttribute(string id = "", string? eventSequence = default) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for an observer.
    /// </summary>
    public ObserverId Id { get; } = id;

    /// <summary>
    /// Gets the unique identifier for an event log.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequence ?? EventSequenceId.Log;
}
