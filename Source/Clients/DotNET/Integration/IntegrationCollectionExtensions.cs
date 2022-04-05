// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Integration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for using Aksio.Cratis with a <see cref="IServiceCollection"/>.
/// </summary>
public static class IntegrationCollectionExtensions
{
    /// <summary>
    /// Configure use of integration.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to build on.</param>
    /// <returns><see cref="IServiceCollection"/> for configuration continuation.</returns>
    public static IServiceCollection AddIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IHostedService, AdaptersService>();
        return services;
    }
}
