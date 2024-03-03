// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Connections;
using Cratis.EventSequences;

namespace Cratis.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="ConnectionId"><see cref="ConnectionId"/> identifying the actual connection.</param>
public record ConnectedObserverKey(
    MicroserviceId MicroserviceId,
    TenantId TenantId,
    EventSequenceId EventSequenceId,
    ConnectionId ConnectionId)
{
    /// <summary>
    /// Gets the empty <see cref="ObserverKey"/>.
    /// </summary>
    public static readonly ConnectedObserverKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet, EventSequenceId.Unspecified, ConnectionId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ConnectedObserverKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{ConnectionId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ConnectedObserverKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        var connectionId = (ConnectionId)elements[3];
        return new(microserviceId, tenantId, eventSequenceId, connectionId);
    }
}
