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
    bool _addedKernelMicroservice;
    StorageType? _cluster;
    StorageForMicroservices? _storageForMicroservices;

    /// <summary>
    /// The storage configuration for the cluster.
    /// </summary>
    public StorageType Cluster
    {
        get => _cluster ?? new();
        init
        {
            _cluster = value;
            AddKernelMicroserviceIfNotAdded();
        }
    }

    /// <summary>
    /// The storage configuration for all microservices.
    /// </summary>
    public StorageForMicroservices Microservices
    {
        get => _storageForMicroservices ?? new();
        init
        {
            _storageForMicroservices = value;
            AddKernelMicroserviceIfNotAdded();
        }
    }

    void AddKernelMicroserviceIfNotAdded()
    {
        if (_addedKernelMicroservice || _storageForMicroservices is null || _cluster is null) return;

        var tenants = _storageForMicroservices!.First().Value.Tenants.Select(_ => _.Key);
        _storageForMicroservices[MicroserviceId.Kernel] = new()
        {
            Shared = new StorageTypes
            {
                ["eventStore"] = _cluster,
            },
            Tenants = new()
        };

        foreach (var tenant in tenants)
        {
            _storageForMicroservices[MicroserviceId.Kernel].Tenants[tenant] = new StorageTypes
            {
                ["readModels"] = _cluster,
                ["eventStore"] = _cluster
            };
        }

        _addedKernelMicroservice = true;
    }
}
