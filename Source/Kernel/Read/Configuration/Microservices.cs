// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.Configuration;
using Microsoft.AspNetCore.Mvc;
using ApiMicroservice = Aksio.Cratis.Kernel.Read.Configuration.Microservices.Microservice;

namespace Aksio.Cratis.Kernel.Read.Configuration.Microservices;

/// <summary>
/// Represents the API for working with the configuration of the kernel.
/// </summary>
[Route("/api/configuration/microservices")]
public class Microservices : Controller
{
    readonly KernelConfiguration _configuration;
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microservices"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Microservices(
        KernelConfiguration configuration,
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager)
    {
        _configuration = configuration;
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
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
    public async Task<StorageForMicroservice> StorageConfigurationForMicroservice([FromRoute] MicroserviceId microserviceId)
    {
        _executionContextManager.Establish(microserviceId);
        var grain = _grainFactory.GetGrain<IConfiguration>(Guid.Empty);
        return await grain.GetStorage();
    }
}
