// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Extension methods for configuring Reactors.
/// </summary>
public static class ReactorsConfigurationExtensions
{
    /// <summary>
    /// Add Reactors to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <returns>Continuation.</returns>
    public static IServiceCollection AddReactors(this IServiceCollection services, IClientArtifactsProvider clientArtifacts)
    {
        clientArtifacts.Reactors.ForEach(_ => services.AddTransient(_));
        return services;
    }
}
