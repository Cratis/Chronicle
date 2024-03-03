// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;
using Cratis.Kernel.Orleans.Configuration;

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration for the Kernel.
/// </summary>
[Configuration("cratis")]
public class KernelConfiguration : IPerformPostBindOperations
{
    /// <summary>
    /// Gets the <see cref="Tenants"/> configuration.
    /// </summary>
    public Tenants Tenants { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Microservice"/> configuration.
    /// </summary>
    public Microservices Microservices { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Cluster"/> configuration.
    /// </summary>
    public Cluster Cluster { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Telemetry"/> configuration.
    /// </summary>
    public Telemetry Telemetry { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    public Storage Storage { get; init; } = new();

    /// <inheritdoc/>
    public void Perform()
    {
        Tenants[TenantId.NotSet.ToString()] = new()
        {
            Name = "Default Single Tenant"
        };
        Microservices[MicroserviceId.Unspecified.ToString()] = new()
        {
            Name = "Shared"
        };
        Microservices[MicroserviceId.Unspecified.ToString()] = new() { Name = "Shared" };
        Microservices[MicroserviceId.Kernel.ToString()] = new() { Name = "Kernel" };
        Storage.ConfigureKernelMicroservice(Tenants.Select(_ => _.Key));
    }
}
