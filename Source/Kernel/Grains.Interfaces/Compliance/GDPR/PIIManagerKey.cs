// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Grains.Compliance.GDPR;

/// <summary>
/// Represents the key for <see cref="IPIIManager"/>.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
public record PIIManagerKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly PIIManagerKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="PIIManagerKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(PIIManagerKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PIIManagerKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator PIIManagerKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a <see cref="PIIManagerKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="PIIManagerKey"/>.</returns>
    public static PIIManagerKey Parse(string key)
    {
        var part = key.Split('+');
        return new PIIManagerKey(part[0], part[1]);
    }
}
