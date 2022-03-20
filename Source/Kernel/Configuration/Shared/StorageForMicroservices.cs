// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents all storage configurations for all <see cref="MicroserviceId">microservices</see> in the system.
/// </summary>
public class StorageForMicroservices : Dictionary<string, StorageForMicroservice>
{
    /// <summary>
    /// Get a specific <see cref="MicroserviceId"/>.
    /// </summary>
    /// <param name="microservice">Type of storage to get.</param>
    /// <returns><see cref="StorageForMicroservice"/> instance.</returns>
    public StorageForMicroservice Get(MicroserviceId microservice) => this[microservice];
}
