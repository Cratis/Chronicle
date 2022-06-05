// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Outbox;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for using Aksio.Cratis with a <see cref="IServiceCollection"/>.
/// </summary>
public static class OutboxProjectionsServiceCollectionExtensions
{
    /// <summary>
    /// Configure use of projections.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
    /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
    public static IServiceCollection AddOutboxProjections(this IServiceCollection services)
    {
        services.AddSingleton<IHostedService, OutboxProjectionsService>();
        return services;
    }
}
