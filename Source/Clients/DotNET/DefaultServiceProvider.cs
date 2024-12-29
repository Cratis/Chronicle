// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a default <see cref="IServiceProvider"/> that will create instances of services using the default constructor.
/// </summary>
public class DefaultServiceProvider : IServiceProvider, IServiceProviderIsService, IServiceScopeFactory, IServiceScope
{
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider => this;

    /// <inheritdoc/>
    public IServiceScope CreateScope() => this;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProviderIsService)) return this;
        if (serviceType == typeof(IServiceScopeFactory)) return this;
        return Activator.CreateInstance(serviceType);
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType) => true;
}
