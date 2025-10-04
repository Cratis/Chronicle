// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Applications.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for aggregate roots.
/// </summary>
public static class AggregateRootServiceCollectionExtensions
{
    /// <summary>
    /// Adds aggregate root auto-discovery and registration to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> for type discovery.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddAggregateRoots(this IServiceCollection services, IClientArtifactsProvider clientArtifactsProvider)
    {
        foreach (var aggregateRootType in clientArtifactsProvider.AggregateRoots)
        {
            services.AddTransient(aggregateRootType, serviceProvider =>
            {
                var commandContext = serviceProvider.GetRequiredService<CommandContext>();
                var aggregateRootFactory = serviceProvider.GetRequiredService<IAggregateRootFactory>();

                var eventSourceIdProperty = commandContext.Command
                    .GetType()
                    .GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsAssignableTo(typeof(EventSourceId)) ||
                                          p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0) ??
                    throw new UnableToResolveAggregateRootFromCommandContext(aggregateRootType);

                EventSourceId eventSourceId;
                if (eventSourceIdProperty.PropertyType.IsAssignableTo(typeof(EventSourceId)))
                {
                    eventSourceId = (EventSourceId)eventSourceIdProperty.GetValue(commandContext.Command)!;
                }
                else
                {
                    var propertyValue = eventSourceIdProperty.GetValue(commandContext.Command);
                    eventSourceId = propertyValue?.ToString() ?? EventSourceId.Unspecified;
                }
                if (eventSourceId == EventSourceId.Unspecified)
                {
                    throw new UnableToResolveAggregateRootFromCommandContext(aggregateRootType);
                }

                var getMethod = typeof(IAggregateRootFactory)
                    .GetMethods()
                    .First(m => m.Name == nameof(IAggregateRootFactory.Get) && m.IsGenericMethod);

                var genericGetMethod = getMethod.MakeGenericMethod(aggregateRootType);
                return genericGetMethod.Invoke(aggregateRootFactory, [eventSourceId, null, null])!;
            });
        }

        return services;
    }
}
