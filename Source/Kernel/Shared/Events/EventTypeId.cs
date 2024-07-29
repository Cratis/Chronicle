// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the concept of the unique identifier of a type of event.
/// </summary>@
/// <param name="Value">Actual value.</param>
public record EventTypeId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the identifier for an unknown event type.
    /// </summary>
    public static readonly EventTypeId Unknown = string.Empty;

    /// <summary>
    /// Implicitly convert from string to <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator EventTypeId(string id) => new(id);
}
