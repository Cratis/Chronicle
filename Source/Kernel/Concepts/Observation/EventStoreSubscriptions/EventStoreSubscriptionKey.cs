// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the compound key for an event store subscription grain.
/// </summary>
/// <param name="SubscriptionId">The subscription identifier.</param>
/// <param name="TargetEventStore">The target event store.</param>
public record EventStoreSubscriptionKey(EventStoreSubscriptionId SubscriptionId, EventStoreName TargetEventStore)
{
    /// <summary>
    /// Implicitly convert from <see cref="EventStoreSubscriptionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="EventStoreSubscriptionKey"/> to convert from.</param>
    public static implicit operator string(EventStoreSubscriptionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(SubscriptionId, TargetEventStore);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="EventStoreSubscriptionKey"/> instance.</returns>
    public static EventStoreSubscriptionKey Parse(string key) => KeyHelper.Parse<EventStoreSubscriptionKey>(key);
}
