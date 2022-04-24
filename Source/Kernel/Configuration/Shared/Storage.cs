// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public StorageType Cluster { get; init; } = new();

    /// <summary>
    /// The storage configuration for all microservices.
    /// </summary>
    public StorageForMicroservices Microservices { get; init; } = new();
}
