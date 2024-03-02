// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The event source identifier - partition.</param>
/// <param name="SourceMicroserviceId">Optional source Microservice identifier. Typically used for Inbox.</param>
/// <param name="SourceTenantId">Optional source Tenant identifier. Typically used for Inbox.</param>
public record ObserverSubscriberKey(
    MicroserviceId MicroserviceId,
    TenantId TenantId,
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    MicroserviceId? SourceMicroserviceId = default,
    TenantId? SourceTenantId = default)
{
    /// <summary>
    /// Implicitly convert from <see cref="ObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserverSubscriberKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (SourceMicroserviceId is not null && SourceTenantId is not null)
        {
            return $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{EventSourceId}+{SourceMicroserviceId}+{SourceTenantId}";
        }
        return $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{EventSourceId}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserverSubscriberKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        var eventSourceId = (EventSourceId)elements[3];
        MicroserviceId? sourceMicroserviceId = null;
        TenantId? sourceTenantId = null;
        if (elements.Length > 4)
        {
            sourceMicroserviceId = (MicroserviceId)elements[4];
            sourceTenantId = (TenantId)elements[5];
        }
        return new(microserviceId, tenantId, eventSequenceId, eventSourceId, sourceMicroserviceId, sourceTenantId);
    }

    /// <summary>
    /// Creates an ObserverSubscriberKey from an ObserverKey and an EventSourceId.
    /// </summary>
    /// <param name="observerKey">The Observer Subscriber Key.</param>
    /// <param name="eventSourceId">The EventSourceId.</param>
    /// <returns>An ObserverSubscriber Key.</returns>
    public static ObserverSubscriberKey FromObserverKey(ObserverKey observerKey, EventSourceId eventSourceId)
        => new(observerKey.MicroserviceId, observerKey.TenantId, observerKey.EventSequenceId, eventSourceId, observerKey.SourceMicroserviceId, observerKey.SourceTenantId);
}
