// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis;

/// <summary>
/// Represents a combination of an <see cref="EventStoreName"/> and an <see cref="EventStoreNamespaceName"/>.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
public record EventStoreAndNamespace(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// The value representing the <see cref="EventStoreAndNamespace"/> not being set.
    /// </summary>
    public static readonly EventStoreAndNamespace NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="EventStoreAndNamespace"/>.
    /// </summary>
    /// <param name="input">String to parse.</param>
    public static implicit operator EventStoreAndNamespace(string input) => Parse(input);

    /// <summary>
    /// Implicitly convert to string.
    /// </summary>
    /// <param name="key">Key to convert.</param>
    public static implicit operator string(EventStoreAndNamespace key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a string representation of <see cref="EventStoreAndNamespace"/>.
    /// </summary>
    /// <param name="input">String to parse.</param>
    /// <returns>Parsed version.</returns>
    public static EventStoreAndNamespace Parse(string input)
    {
        var segments = input.Split('+');
        return new(segments[0], segments[1]);
    }
}
