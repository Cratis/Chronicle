// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Checks registrations without constructing metadata providers or swallowing activation failures.
/// </summary>
internal static class SchemaProviderSharing
{
    /// <summary>
    /// Determines whether schema providers have no store-specific service registrations.
    /// </summary>
    /// <param name="services">The store's services.</param>
    /// <param name="artifacts">The metadata provider catalog.</param>
    /// <returns>Whether structural schemas can be shared.</returns>
    internal static bool IsSafe(IServiceProvider? services, IClientArtifactsProvider artifacts)
    {
        if (services is null || services.GetType() == typeof(DefaultServiceProvider)) return true;
        if (services is DefaultServiceProvider) return false;
        var registrations = services as IServiceProviderIsService ?? services.GetService<IServiceProviderIsService>();
        if (registrations is null) return false;
        return !artifacts.ComplianceForTypesProviders
            .Concat(artifacts.ComplianceForPropertiesProviders)
            .Concat(artifacts.SecurityForTypesProviders)
            .Concat(artifacts.SecurityForPropertiesProviders)
            .Any(type => registrations.IsService(type) || type.GetConstructors().Any(constructor => constructor.GetParameters().Length > 0));
    }
}
