// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Extensions for configuring discovered Chronicle client services.
/// </summary>
public static class ChronicleClientServicesExtensions
{
    /// <summary>
    /// Configure discovered client service extensions in this container.
    /// </summary>
    /// <param name="services">The client service collection.</param>
    /// <returns>The service collection for continuation.</returns>
    public static IServiceCollection ConfigureChronicleClientServices(this IServiceCollection services)
    {
        foreach (var type in TypeUniverse.Current.FindMultiple<IChronicleClientServices>())
        {
            ((IChronicleClientServices)Activator.CreateInstance(type)!).Configure(services);
        }

        return services;
    }
}
