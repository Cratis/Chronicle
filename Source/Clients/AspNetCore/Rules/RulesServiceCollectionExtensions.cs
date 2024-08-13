// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Rules;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working adding rules to <see cref="IServiceCollection"/> .
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
        services.Configure<MvcOptions>(options => options.ModelValidatorProviders.Insert(0, new RulesModelValidatorProvider()));

        return services;
    }
}
