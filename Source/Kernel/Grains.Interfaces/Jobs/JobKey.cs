// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the key for a job.
/// </summary>
/// <param name="MicroserviceId">The Microservice the job is for.</param>
/// <param name="TenantId">The tenant the job is for.</param>
public record JobKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly JobKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="JobKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobKey"/> instance.</returns>
    public static JobKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        return new(microserviceId, tenantId);
    }
}
