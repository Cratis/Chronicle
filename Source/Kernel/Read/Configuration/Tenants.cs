// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Read.Configuration.Tenants;

/// <summary>
/// Represents the API for working with tenants.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Microservices"/> class.
/// </remarks>
/// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
[Route("/api/configuration/tenants")]
public class Tenants(KernelConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Get all the tenants.
    /// </summary>
    /// <returns>Collection of <see cref="TenantInfo"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<TenantInfo>> AllTenants() =>
       Task.FromResult(configuration.Tenants.Select(_ => new TenantInfo(_.Key, _.Value.Name)).ToArray().AsEnumerable());
}
