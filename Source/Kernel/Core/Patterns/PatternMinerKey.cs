// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents the key for a pattern miner.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
/// <remarks>
/// Behavior belongs to a scope inside an event store's namespace, so the miner is one grain per event store and
/// namespace - the key carries exactly those two parts. The same scope name in two stores, or two tenants'
/// namespaces, resolves to two different grains and can never count into one sketch.
/// </remarks>
public record PatternMinerKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly PatternMinerKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="PatternMinerKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(PatternMinerKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PatternMinerKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator PatternMinerKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace);

    /// <summary>
    /// Parse a <see cref="PatternMinerKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="PatternMinerKey"/>.</returns>
    public static PatternMinerKey Parse(string key) => KeyHelper.Parse<PatternMinerKey>(key);
}
