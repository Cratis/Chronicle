// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Extension methods for configuring reactions.
/// </summary>
public static class ReactionsConfigurationExtensions
{
    /// <summary>
    /// Add reactions to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <returns>Continuation.</returns>
    public static IServiceCollection AddReactions(this IServiceCollection services, IClientArtifactsProvider clientArtifacts)
    {
        clientArtifacts.Reactions.ForEach(_ => services.AddTransient(_));
        return services;
    }
}
