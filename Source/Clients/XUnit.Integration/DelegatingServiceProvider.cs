// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Delegates service resolution to the current test-specific service provider first and
/// then falls back to the shared root provider from the reused host.
/// </summary>
/// <param name="rootServiceProvider">The shared root <see cref="IServiceProvider"/>.</param>
internal class DelegatingServiceProvider(IServiceProvider rootServiceProvider) : IServiceProvider, IServiceProviderIsService, IServiceScopeFactory
{
    static readonly object _lock = new();
    static DelegatingServiceProvider? _instance;
    static IServiceProvider? _currentServiceProvider;

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider) ||
            serviceType == typeof(IServiceProviderIsService) ||
            serviceType == typeof(IServiceScopeFactory))
        {
            return this;
        }

        var service = _currentServiceProvider?.GetService(serviceType);
        return service ?? rootServiceProvider.GetService(serviceType);
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider) ||
            serviceType == typeof(IServiceProviderIsService) ||
            serviceType == typeof(IServiceScopeFactory))
        {
            return true;
        }

        if ((_currentServiceProvider as IServiceProviderIsService)?.IsService(serviceType) == true)
        {
            return true;
        }

        return (rootServiceProvider as IServiceProviderIsService)?.IsService(serviceType) == true;
    }

    /// <inheritdoc/>
    public IServiceScope CreateScope()
    {
        var rootScope = (rootServiceProvider as IServiceScopeFactory)?.CreateScope();
        var currentScope = (_currentServiceProvider as IServiceScopeFactory)?.CreateScope();
        return new DelegatingServiceScope(rootScope, currentScope);
    }

    /// <summary>
    /// Gets the existing singleton instance or creates it with the provided root provider.
    /// </summary>
    /// <param name="rootServiceProvider">The shared root <see cref="IServiceProvider"/>.</param>
    /// <returns>The shared <see cref="DelegatingServiceProvider"/> instance.</returns>
    internal static DelegatingServiceProvider GetOrCreate(IServiceProvider rootServiceProvider)
    {
        lock (_lock)
        {
            _instance ??= new(rootServiceProvider);
            return _instance;
        }
    }

    /// <summary>
    /// Sets the current test-specific <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="serviceProvider">The current test-specific <see cref="IServiceProvider"/>.</param>
    internal static void SetCurrent(IServiceProvider serviceProvider)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_currentServiceProvider, serviceProvider))
            {
                return;
            }

            (_currentServiceProvider as IDisposable)?.Dispose();
            _currentServiceProvider = serviceProvider;
        }
    }
}
