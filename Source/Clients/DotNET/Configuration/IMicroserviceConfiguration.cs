// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Shared.Configuration;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Defines the client for working with microservice configuration.
/// </summary>
public interface IMicroserviceConfiguration
{
    /// <summary>
    /// Gets the storage configuration for the current microservice.
    /// </summary>
    /// <returns><see cref="StorageForMicroservice"/>.</returns>
    Task<StorageForMicroservice> Storage();
}
