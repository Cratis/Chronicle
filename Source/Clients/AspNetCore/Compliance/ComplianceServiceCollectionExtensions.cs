// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working adding compliance to <see cref="IServiceCollection"/> .
/// </summary>
public static class ComplianceServiceCollectionExtensions
{
    /// <summary>
    /// Add Unit of work support.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCompliance(this IServiceCollection services)
    {
        services.AddSingleton<ICanProvideComplianceMetadataForProperty, PIIMetadataProvider>();
        services.AddSingleton<ICanProvideComplianceMetadataForType, PIIMetadataProvider>();

        return services;
    }
}
