// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.AspNetCore.Rules;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with <see cref="MvcOptions"/>.
/// </summary>
public static class RulesServiceCollectionExtensions
{
    /// <summary>
    /// Add CQRS setup.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="MvcOptions"/> for building continuation.</returns>
    public static IServiceCollection AddRules(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            options.ModelValidatorProviders.Add(new RulesModelValidatorProvider());
        });

        return services;
    }
}
