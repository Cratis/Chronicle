// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for registering Chronicle client artifacts.
/// </summary>
public static class ClientArtifactsServiceCollectionExtensions
{
    /// <summary>
    /// Add Chronicle client artifacts to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for discovering artifacts.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleArtifacts(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        clientArtifactsProvider.Initialize();

        clientArtifactsProvider.Projections.ForEach(_ => services.AddTransient(_));
        clientArtifactsProvider.Reactors.ForEach(_ => services.AddScoped(_));
        clientArtifactsProvider.Reducers.ForEach(_ => services.AddScoped(_));
        clientArtifactsProvider.ReactorMiddlewares.ForEach(_ => services.AddTransient(_));
        clientArtifactsProvider.ConstraintTypes.ForEach(_ => services.AddTransient(_));
        clientArtifactsProvider.EventSeeders.ForEach(_ => services.AddTransient(_));

        return services;
    }

    /// <summary>
    /// Add Chronicle client artifacts to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="chronicleOptions"><see cref="ChronicleOptions"/> containing the artifacts provider.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleArtifacts(this IServiceCollection services, ChronicleOptions chronicleOptions)
    {
        return services.AddCratisChronicleArtifacts(chronicleOptions.ArtifactsProvider);
    }

    /// <summary>
    /// Add Chronicle client artifacts to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="chronicleClient"><see cref="IChronicleClient"/> to get the artifacts provider from.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChronicleArtifacts(this IServiceCollection services, IChronicleClient chronicleClient)
    {
        return services.AddCratisChronicleArtifacts(chronicleClient.Options.ArtifactsProvider);
    }
}
