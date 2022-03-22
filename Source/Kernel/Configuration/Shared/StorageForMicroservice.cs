// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents all storage configurations for all <see cref="MicroserviceId">microservices</see> in the system.
/// </summary>
public class StorageForMicroservice
{
    /// <summary>
    /// The shared database connection configurations for the microservice.
    /// </summary>
    public StorageTypesConfig Shared { get; init; } = new();

    /// <summary>
    /// The tenant specific configuration.
    /// </summary>
    public StorageForTenants Tenants { get; init; } = new();
}
