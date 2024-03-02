// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Configuration.Tenants;

/// <summary>
/// Represents the API for working with tenants.
/// </summary>
[Route("/api/configuration/tenants")]
public class Tenants : ControllerBase
{
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microservices"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public Tenants(KernelConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get all the tenants.
    /// </summary>
    /// <returns>Collection of <see cref="TenantInfo"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<TenantInfo>> AllTenants() =>
       Task.FromResult(_configuration.Tenants.Select(_ => new TenantInfo(_.Key, _.Value.Name)).ToArray().AsEnumerable());
}
