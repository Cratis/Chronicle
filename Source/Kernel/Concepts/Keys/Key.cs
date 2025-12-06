// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys;

/// <summary>
/// Represents the key coming from an event.
/// </summary>
/// <param name="Value">The actual key value.</param>
/// <param name="ArrayIndexers">Any array indexers.</param>
public record Key(object Value, ArrayIndexers ArrayIndexers)
{
    /// <summary>
    /// Gets the <see cref="Key"/> representing an unset key.
    /// </summary>
    public static readonly Key Undefined = new(null!, ArrayIndexers.NoIndexers);

    /// <summary>
    /// Implicitly convert from a <see cref="EventSourceId"/> to a <see cref="Key"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to convert from.</param>
    public static implicit operator Key(EventSourceId eventSourceId) => new(eventSourceId.Value, ArrayIndexers.NoIndexers);

    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="Key"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator Key(string value) => new(value, ArrayIndexers.NoIndexers);

    /// <summary>
    /// Implicitly convert from a <see cref="Key"/> to a <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="key"><see cref="Key"/> to convert from.</param>
    public static implicit operator EventSourceId(Key key) => new(key.Value?.ToString() ?? EventSourceId.Unspecified);

    /// <inheritdoc/>
    public override string ToString() => Value?.ToString() ?? base.ToString() ?? string.Empty;

    /// <summary>
    /// Determines whether the key is equal to the provided value.
    /// </summary>
    /// <param name="value">Value to compare to.</param>
    /// <returns>True if the key is equal to the provided value; otherwise, false.</returns>
    public bool IsEqualTo(object value) => Value.Equals(value);
}
