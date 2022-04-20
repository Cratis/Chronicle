// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using ApiMicroservice = Aksio.Cratis.Configuration.Api.Microservice;
using ApiTenant = Aksio.Cratis.Configuration.Api.Tenant;

namespace Aksio.Cratis.Configuration.Api;

/// <summary>
/// Represents the API for working with the configuration of the kernel.
/// </summary>
[Route("/api/configuration")]
public class AllConfiguration : Controller
{
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllConfiguration"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public AllConfiguration(KernelConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns all the tenants configured in the kernel.
    /// </summary>
    /// <returns>Collection of <see cref="Configuration.Tenant"/>.</returns>
    [HttpGet("tenants")]
    public IEnumerable<ApiTenant> Tenants() => _configuration.Tenants.Select(kvp => new ApiTenant(kvp.Key, kvp.Value.Name));

    /// <summary>
    /// Returns all the tenants configured in the kernel.
    /// </summary>
    /// <returns>Collection of <see cref="Configuration.Tenant"/>.</returns>
    [HttpGet("microservices")]
    public IEnumerable<ApiMicroservice> Microservices() => _configuration.Microservices.Select(kvp => new ApiMicroservice(kvp.Key, kvp.Value.Name));
}
