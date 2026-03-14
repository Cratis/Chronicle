// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Represents the unique identifier of an event store subscription on the client.
/// </summary>
/// <param name="Value">The actual value.</param>
public record EventStoreSubscriptionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    public static readonly EventStoreSubscriptionId Unspecified = ObserverId.Unspecified;

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator EventStoreSubscriptionId(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="EventStoreSubscriptionId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="EventStoreSubscriptionId"/> to convert from.</param>
    public static implicit operator ObserverId(EventStoreSubscriptionId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="EventStoreSubscriptionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator EventStoreSubscriptionId(ObserverId id) => new(id.Value);
}
