// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to adorn types for providing metadata about the actual <see cref="EventType"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventTypeAttribute"/>.
/// </remarks>
/// <param name="idAsGuid"><see cref="EventTypeId">Identifier</see> as string representation of a <see cref="Guid"/>.</param>
/// <param name="generation"><see cref="EventGeneration"/> represented as <see cref="uint"/>.</param>
/// <param name="isPublic">Whether or not the event type is for public use.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventTypeAttribute(string idAsGuid, uint generation = EventGeneration.FirstValue, bool isPublic = false) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventType"/>.
    /// </summary>
    public EventType Type { get; } = new(Guid.Parse(idAsGuid), generation, isPublic);

    /// <summary>
    /// Gets or sets whether or not this event type should be available publicly.
    /// </summary>
    public bool IsPublic { get; } = isPublic;
}
