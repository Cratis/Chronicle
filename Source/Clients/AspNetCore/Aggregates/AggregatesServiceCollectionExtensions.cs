// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for setting up aggregate roots.
/// </summary>
public static class AggregatesServiceCollectionExtensions
{
    /// <summary>
    /// Add causation.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to extend.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddAggregates(this IServiceCollection services)
    {
        services.AddScoped(sp =>
        {
            var eventStore = sp.GetRequiredService<IEventStore>();
            return eventStore.AggregateRootFactory;
        });

        return services;
    }
}
