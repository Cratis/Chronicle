// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
public record ObserversKey(
    MicroserviceId MicroserviceId,
    TenantId TenantId)
{
    /// <summary>
    /// Gets the empty <see cref="ObserversKey"/>.
    /// </summary>
    public static readonly ObserversKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="ObserversKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserversKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserversKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];

        return new(microserviceId, tenantId);
    }
}
