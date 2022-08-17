// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the storage configuration for all microservices.
/// </summary>
[Configuration]
public class Storage
{
    /// <summary>
    /// The storage configuration for the cluster.
    /// </summary>
    public StorageType Cluster { get; set; } = new();

    /// <summary>
    /// The storage configuration for all microservices.
    /// </summary>
    public StorageForMicroservices Microservices { get; set; } = new();

    /// <summary>
    /// Configure the Kernel as a Microservice.
    /// </summary>
    /// <param name="tenants">Tenants the Kernel needs to support.</param>
    public void ConfigureKernelMicroservice(IEnumerable<string> tenants)
    {
        Microservices[MicroserviceId.Kernel] = new()
        {
            Shared = new StorageTypes
            {
                ["eventStore"] = Cluster,
            },
            Tenants = new()
        };

        foreach (var tenant in tenants)
        {
            Microservices[MicroserviceId.Kernel].Tenants[tenant] = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster
            };
        }
    }
}
