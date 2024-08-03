// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to adorn types for providing metadata about the actual <see cref="EventType"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventTypeAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier of the event type, if not used it will default to the type name.</param>
/// <param name="generation"><see cref="EventGeneration"/> represented as <see cref="uint"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventTypeAttribute(string id = "", uint generation = EventGeneration.FirstValue) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventTypeId"/> for the event type.
    /// </summary>
    public EventTypeId Id { get; } = id;

    /// <summary>
    /// Gets the <see cref="EventGeneration"/> for the event type.
    /// </summary>
    public EventGeneration Generation { get; } = generation;
}
