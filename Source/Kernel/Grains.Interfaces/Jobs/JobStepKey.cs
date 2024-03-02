// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the key for a job step.
/// </summary>
/// <param name="JobId">The job the step is for.</param>
/// <param name="MicroserviceId">The Microservice the job step is for.</param>
/// <param name="TenantId">The tenant the job step is for.</param>
public record JobStepKey(JobId JobId, MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Implicitly convert from string to <see cref="JobKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator JobStepKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="JobKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(JobStepKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{JobId}+{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="JobKey"/> instance.</returns>
    public static JobStepKey Parse(string key)
    {
        var elements = key.Split('+');
        var jobId = (JobId)elements[0];
        var microserviceId = (MicroserviceId)elements[1];
        var tenantId = (TenantId)elements[2];
        return new(jobId, microserviceId, tenantId);
    }
}
