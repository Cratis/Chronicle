// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the event serializer with its runtime lifetime.
/// </summary>
internal static class EventSerializerServiceCollectionExtensions
{
    /// <summary>
    /// Registers one event serializer for the event store selected by the current scope.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the serializer to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddEventSerializer(this IServiceCollection services)
    {
        services.TryAddScoped<EventSerializer>();
        services.TryAddScoped<IEventSerializer>(serviceProvider => serviceProvider.GetRequiredService<EventSerializer>());

        return services;
    }
}
