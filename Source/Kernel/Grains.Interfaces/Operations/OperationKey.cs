// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Operations;

/// <summary>
/// Represents the key for a job.
/// </summary>
/// <param name="MicroserviceId">The Microservice the job is for.</param>
/// <param name="TenantId">The tenant the job is for.</param>
public record OperationKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly OperationKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="OperationKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator OperationKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="OperationKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(OperationKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="OperationKey"/> instance.</returns>
    public static OperationKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        return new(microserviceId, tenantId);
    }
}
