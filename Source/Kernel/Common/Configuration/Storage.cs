// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;
using Cratis.Configuration;

namespace Cratis.Kernel.Configuration;

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
    public StorageForMicroservices Microservices { get; set; } = [];

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
            Tenants = []
        };

        // We add unspecified for supporting single tenant scenarios
        Microservices[MicroserviceId.Unspecified] = new()
        {
            Shared = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster,
            },
            Tenants = []
        };

        foreach (var microservice in Microservices)
        {
            microservice.Value.Tenants[TenantId.NotSet.ToString()] = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster
            };

            if (!microservice.Value.Tenants.ContainsKey(TenantId.Development.ToString()))
            {
                microservice.Value.Tenants[TenantId.Development.ToString()] = new StorageTypes
                {
                    ["readModels"] = Cluster,
                    ["eventStore"] = Cluster
                };
            }
        }

        foreach (var tenant in tenants)
        {
            Microservices[MicroserviceId.Unspecified].Tenants[tenant] = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster
            };

            Microservices[MicroserviceId.Kernel].Tenants[tenant] = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster
            };

            Microservices[MicroserviceId.Unspecified].Tenants[tenant] = new StorageTypes
            {
                ["readModels"] = Cluster,
                ["eventStore"] = Cluster
            };
        }
    }
}
