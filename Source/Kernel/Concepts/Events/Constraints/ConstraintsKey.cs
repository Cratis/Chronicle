// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a key for constraints.
/// </summary>
/// <param name="EventStore">The event store it belongs to.</param>
public record ConstraintsKey(EventStoreName EventStore)
{
    /// <summary>
    /// Gets the empty <see cref="ConstraintsKey"/>.
    /// </summary>
    public static readonly ConstraintsKey NotSet = new(EventStoreName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="ConstraintsKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ConstraintsKey"/> to convert from.</param>
    public static implicit operator string(ConstraintsKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from string to <see cref="ConstraintsKey"/>.
    /// </summary>
    /// <param name="key">String representation to convert from.</param>
    public static implicit operator ConstraintsKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{EventStore}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ConstraintsKey"/> instance.</returns>
    public static ConstraintsKey Parse(string key)
    {
        var elements = key.Split('+');
        var eventStore = (EventStoreName)elements[0];
        return new(eventStore);
    }
}
