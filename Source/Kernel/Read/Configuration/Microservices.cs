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
[Route("/api/configuration/microservices")]
public class Microservices : ControllerBase
{
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microservices"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public Microservices(KernelConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns all the tenants configured in the kernel.
    /// </summary>
    /// <returns>Collection of <see cref="ApiMicroservice"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ApiMicroservice>> AllMicroservices() => Task.FromResult(_configuration.Microservices.Select(kvp => new ApiMicroservice(kvp.Key, kvp.Value.Name)));

    /// <summary>
    /// Get storage configuration for a specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> for the microservice.</param>
    /// <returns>The <see cref="StorageForMicroservice"/>.</returns>
    [HttpGet("{microserviceId}/storage")]
    public Task<StorageForMicroservice> StorageConfigurationForMicroservice([FromRoute] MicroserviceId microserviceId) =>
        Task.FromResult(_configuration.Storage.Microservices.Get(microserviceId));
}
