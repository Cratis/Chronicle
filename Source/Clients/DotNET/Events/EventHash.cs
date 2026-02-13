// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the hash of an event's content.
/// </summary>
/// <param name="Value">The hash value.</param>
public record EventHash(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the value when the hash is not set.
    /// </summary>
    public static readonly EventHash NotSet = string.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventHash"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator EventHash(string value) => new(value);
}
