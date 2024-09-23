// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore.Transactions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working adding unit of work to <see cref="IServiceCollection"/> .
/// </summary>
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Add Unit of work support.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options => options.Filters.Add<UnitOfWorkActionFilter>(0));
        services.AddTransient<IStartupFilter, UnitOfWorkStartupFilter>();
        services.AddScoped(sp =>
        {
            var eventStore = sp.GetRequiredService<IEventStore>();
            return eventStore.UnitOfWorkManager;
        });

        return services;
    }
}
