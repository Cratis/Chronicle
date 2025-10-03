// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Applications.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;

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
    /// <param name="types">The <see cref="ITypes"/> system for type discovery.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection AddReadModels(this IServiceCollection services, ITypes types)
    {
        var readModelTypes = types
            .All
            .Where(type => type.IsClass &&
                          !type.IsAbstract &&
                          HasProjectionOrReducerFor(type, types) &&
                          type.Namespace?.StartsWith("Cratis.Chronicle.") is not true)
            .ToArray();

        foreach (var readModelType in readModelTypes)
        {
            services.AddTransient(readModelType, serviceProvider =>
            {
                var commandContext = serviceProvider.GetRequiredService<CommandContext>();
                var projections = serviceProvider.GetRequiredService<IProjections>();

                var keyProperty = commandContext.Command
                    .GetType()
                    .GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsAssignableTo(typeof(EventSourceId)) ||
                                          p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0) ??
                    throw new UnableToResolveReadModelFromCommandContext(readModelType);

                ReadModelKey readModelKey;
                if (keyProperty.PropertyType.IsAssignableTo(typeof(EventSourceId)))
                {
                    readModelKey = (EventSourceId)keyProperty.GetValue(commandContext.Command)!;
                }
                else
                {
                    var propertyValue = keyProperty.GetValue(commandContext.Command);
                    readModelKey = propertyValue?.ToString() ?? ReadModelKey.Unspecified;
                }

                if (readModelKey == ReadModelKey.Unspecified)
                {
                    throw new UnableToResolveReadModelFromCommandContext(readModelType);
                }

                var result = projections.GetInstanceById(readModelType, readModelKey).GetAwaiter().GetResult();
                return result.ReadModel;
            });
        }

        return services;
    }

    static bool HasProjectionOrReducerFor(Type readModelType, ITypes types)
    {
        return types.All.Any(type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjectionFor<>) && i.GetGenericArguments()[0] == readModelType));
    }
}
