// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Delegates scoped service resolution to the current test scope first and then the root scope.
/// </summary>
/// <param name="rootServiceProvider">The shared root scoped <see cref="IServiceProvider"/>.</param>
/// <param name="currentServiceProvider">The current test scoped <see cref="IServiceProvider"/>.</param>
internal class ScopedDelegatingServiceProvider(IServiceProvider? rootServiceProvider, IServiceProvider? currentServiceProvider)
    : IServiceProvider, IServiceProviderIsService
{
    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceProviderIsService))
        {
            return this;
        }

        var service = currentServiceProvider?.GetService(serviceType);
        return service ?? rootServiceProvider?.GetService(serviceType);
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceProviderIsService))
        {
            return true;
        }

        if ((currentServiceProvider as IServiceProviderIsService)?.IsService(serviceType) == true)
        {
            return true;
        }

        return (rootServiceProvider as IServiceProviderIsService)?.IsService(serviceType) == true;
    }
}
