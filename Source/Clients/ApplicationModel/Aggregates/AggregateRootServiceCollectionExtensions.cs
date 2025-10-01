// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Applications.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Types;

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
    /// <param name="types">The <see cref="ITypes"/> system for type discovery.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddAggregateRoots(this IServiceCollection services, ITypes types)
    {
        var aggregateRootTypes = types
            .All
            .Where(type => type.IsClass &&
                          !type.IsAbstract &&
                          type.GetInterfaces().Any(i => i == typeof(IAggregateRoot)) &&
                          type.Namespace?.StartsWith("Cratis.Chronicle.") is not true)
            .ToArray();

        foreach (var aggregateRootType in aggregateRootTypes)
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
                    .First(m => m.Name == "Get" && m.IsGenericMethod);

                var genericGetMethod = getMethod.MakeGenericMethod(aggregateRootType);
                return genericGetMethod.Invoke(aggregateRootFactory, [eventSourceId, null, null])!;
            });
        }

        return services;
    }
}
