// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Setup;

/// <summary>Registers the optional generation catalog for supporting providers.</summary>
public static class GenerationCatalogServiceExtensions
{
    /// <summary>Adds key resolution and the initializer's catalog adapter.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The services for further configuration.</returns>
    public static IServiceCollection AddGenerationCatalog(this IServiceCollection services)
    {
        services.TryAddTransient<IMaterializationKeys, MaterializationKeys>();
        services.TryAddTransient<IGenerationCatalogSeam, GenerationCatalog>();
        return services;
    }
}
