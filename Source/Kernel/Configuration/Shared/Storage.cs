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
    /// The configuration per microservice.
    /// </summary>
    public StorageForMicroservices Microservices { get; init; } = new();

    /// <summary>
    /// The shared database connection configurations for the system.
    /// </summary>
    public SharedStorageForSystem Shared { get; init; } = new();
}
