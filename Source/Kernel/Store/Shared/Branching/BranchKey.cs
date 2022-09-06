// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.Branching;

/// <summary>
/// Represents a key for an inbox.
/// </summary>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="MicroserviceId">The Microservice identifier.</param>
public record BranchKey(TenantId TenantId, MicroserviceId MicroserviceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="BranchKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="BranchKey"/> to convert from.</param>
    public static implicit operator string(BranchKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{TenantId}+{MicroserviceId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="BranchKey"/> instance.</returns>
    public static BranchKey Parse(string key)
    {
        var elements = key.Split('+');
        var tenantId = (TenantId)elements[0];
        var microserviceId = (MicroserviceId)elements[1];
        return new(tenantId, microserviceId);
    }
}
