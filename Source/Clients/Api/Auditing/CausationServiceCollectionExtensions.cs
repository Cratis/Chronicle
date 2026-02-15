// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Api.Auditing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for setting up causation.
/// </summary>
public static class CausationServiceCollectionExtensions
{
    /// <summary>
    /// Add causation.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to extend.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCausation(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, CausationStartupFilter>();
        services.AddSingleton(sp => sp.GetRequiredService<IChronicleClient>().CausationManager);
        return services;
    }
}
