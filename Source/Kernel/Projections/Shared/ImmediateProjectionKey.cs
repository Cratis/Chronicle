// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents the compound key for an immediate projection.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="ModelKey">The event source identifier.</param>
public record ImmediateProjectionKey(MicroserviceId MicroserviceId, TenantId TenantId, EventSequenceId EventSequenceId, ModelKey ModelKey)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ImmediateProjectionKey"/> to convert from.</param>
    public static implicit operator string(ImmediateProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{ModelKey}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ImmediateProjectionKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        var modelKey = (ModelKey)elements[3];
        return new(microserviceId, tenantId, eventSequenceId, modelKey);
    }
}
