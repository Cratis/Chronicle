// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a key for a <see cref="IJobsManager"/>.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
public record JobsManagerKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Gets the not set <see cref="JobsManagerKey"/>.
    /// </summary>
    public static readonly JobsManagerKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="JobsManagerKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="JobsManagerKey"/> to convert from.</param>
    public static implicit operator string(JobsManagerKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from string to <see cref="JobsManagerKey"/>.
    /// </summary>
    /// <param name="key">String representation to convert from.</param>
    public static implicit operator JobsManagerKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="JobsManagerKey"/> instance.</returns>
    public static JobsManagerKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        return new JobsManagerKey(microserviceId, tenantId);
    }
}
