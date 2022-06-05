// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="SourceMicroserviceId">Optional source Microservice identifier. Typically used for Inbox.</param>
/// <param name="SourceTenantId">Optional source Tenant identifier. Typically used for Inbox.</param>
public record ObserverKey(
    MicroserviceId MicroserviceId,
    TenantId TenantId,
    EventSequenceId EventSequenceId,
    MicroserviceId? SourceMicroserviceId = default,
    TenantId? SourceTenantId = default)
{
    /// <summary>
    /// Implicitly convert from <see cref="ObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserverKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (SourceMicroserviceId is not null && SourceTenantId is not null)
        {
            return $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{SourceMicroserviceId}+{SourceTenantId}";
        }

        return $"{MicroserviceId}+{TenantId}+{EventSequenceId}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserverKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        MicroserviceId? sourceMicroserviceId = null;
        TenantId? sourceTenantId = null;
        if (elements.Length > 3)
        {
            sourceMicroserviceId = (MicroserviceId)elements[3];
            sourceTenantId = (TenantId)elements[4];
        }

        return new(microserviceId, tenantId, eventSequenceId, sourceMicroserviceId, sourceTenantId);
    }
}
