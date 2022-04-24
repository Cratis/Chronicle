// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the storage configuration for all microservices.
/// </summary>
public class StorageForMicroservices : Dictionary<string, StorageForMicroservice>
{
    /// <summary>
    /// Get a specific <see cref="StorageForMicroservice"/>.
    /// </summary>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <returns><see cref="StorageForMicroservice"/> instance.</returns>
    public StorageForMicroservice Get(MicroserviceId microserviceId) => this[microserviceId];
}
