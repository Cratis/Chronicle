// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Transactions;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working adding unit of work to <see cref="IServiceCollection"/> .
/// </summary>
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Add CQRS setup.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="MvcOptions"/> for building continuation.</returns>
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options => options.Filters.Add<UnitOfWorkActionFilter>(0));
        services.AddTransient<IStartupFilter, UnitOfWorkStartupFilter>();

        return services;
    }
}
