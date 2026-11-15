// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Setup;

/// <summary>Registers the optional generation catalog for supporting providers.</summary>
public static class GenerationCatalogServiceExtensions
{
    /// <summary>Adds key resolution and the initializer's catalog adapter.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The services for further configuration.</returns>
    /// <remarks>
    /// Register this only from a storage provider whose <see cref="IMaterializationBindingStorage"/> supports first-generation adoption.
    /// Without it the initializer refuses with <see cref="GenerationInitializationBlockerKind.CatalogUnavailable"/> before writing any receipt, so the store keeps legacy semantics.
    /// </remarks>
    public static IServiceCollection AddGenerationCatalog(this IServiceCollection services)
    {
        services.TryAddTransient<IMaterializationKeys, MaterializationKeys>();
        services.TryAddTransient<IGenerationCatalogSeam, GenerationCatalog>();
        return services;
    }
}
