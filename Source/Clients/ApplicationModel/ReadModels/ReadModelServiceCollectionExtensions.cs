// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Applications.Commands;
using Cratis.Chronicle.Applications.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for read models.
/// </summary>
public static class ReadModelServiceCollectionExtensions
{
    /// <summary>
    /// Adds read model auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for type discovery.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddReadModels(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        var readModelTypesFromProjections = clientArtifactsProvider.Projections
            .Select(projectionType =>
            {
                var projectionInterface = projectionType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionFor<>));
                return projectionInterface?.GetGenericArguments()[0];
            })
            .Where(type => type?.IsClass == true && !type.IsAbstract)
            .Cast<Type>();

        var modelBoundReadModels = clientArtifactsProvider.ModelBoundProjections
            .Where(type => type.IsClass && !type.IsAbstract);

        var readModelTypes = readModelTypesFromProjections
            .Concat(modelBoundReadModels)
            .Distinct()
            .ToArray();

        foreach (var readModelType in readModelTypes)
        {
            services.AddTransient(readModelType, serviceProvider =>
            {
                var commandContext = serviceProvider.GetRequiredService<CommandContext>();
                var projections = serviceProvider.GetRequiredService<IProjections>();

                var eventSourceId = commandContext.GetEventSourceId();
                if (eventSourceId == EventSourceId.Unspecified)
                {
                    throw new UnableToResolveReadModelFromCommandContext(readModelType);
                }

                var result = projections.GetInstanceById(readModelType, eventSourceId).GetAwaiter().GetResult();
                return result.ReadModel;
            });
        }

        return services;
    }
}
