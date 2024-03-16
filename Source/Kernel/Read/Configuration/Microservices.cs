// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Cratis.Kernel.Configuration;
using Microsoft.AspNetCore.Mvc;
using ApiMicroservice = Cratis.Kernel.Read.Configuration.Microservices.Microservice;

namespace Cratis.Kernel.Read.Configuration.Microservices;

/// <summary>
/// Represents the API for working with the configuration of the kernel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Microservices"/> class.
/// </remarks>
/// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
[Route("/api/configuration/microservices")]
public class Microservices(KernelConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns all the tenants configured in the kernel.
    /// </summary>
    /// <returns>Collection of <see cref="ApiMicroservice"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ApiMicroservice>> AllMicroservices() => Task.FromResult(configuration.Microservices.Select(kvp => new ApiMicroservice(kvp.Key, kvp.Value.Name)));

    /// <summary>
    /// Get storage configuration for a specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> for the microservice.</param>
    /// <returns>The <see cref="StorageForMicroservice"/>.</returns>
    [HttpGet("{microserviceId}/storage")]
    public Task<StorageForMicroservice> StorageConfigurationForMicroservice([FromRoute] MicroserviceId microserviceId) =>
        Task.FromResult(configuration.Storage.Microservices.Get(microserviceId));
}
