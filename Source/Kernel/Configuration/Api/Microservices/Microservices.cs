// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration.Grains;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using ApiMicroservice = Aksio.Cratis.Configuration.Api.Microservice;

namespace Aksio.Cratis.Configuration.Api;

/// <summary>
/// Represents the API for working with the configuration of the kernel.
/// </summary>
[Route("/api/configuration/microservices")]
public class Microservices : Controller
{
    readonly KernelConfiguration _configuration;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microservices"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    public Microservices(KernelConfiguration configuration, IGrainFactory grainFactory)
    {
        _configuration = configuration;
        _grainFactory = grainFactory;
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
    /// <param name="microserviceId"></param>
    /// <returns></returns>
    [HttpGet("{microserviceId}/storage")]
    public Task<StorageForMicroservice> storageForMicroservice([FromRoute] MicroserviceId microserviceId)
    {
        var grain = _grainFactory.GetGrain<IConfiguration>(Guid.Empty);
        return grain.GetStorage();
    }
}
