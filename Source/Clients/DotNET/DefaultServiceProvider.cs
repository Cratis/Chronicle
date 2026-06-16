// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a default <see cref="IServiceProvider"/> that will create instances of services using the default constructor.
/// </summary>
public class DefaultServiceProvider : IServiceProvider, IServiceProviderIsService, IServiceProviderIsKeyedService, IKeyedServiceProvider, IServiceScopeFactory, IServiceScope
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
        if (serviceType == typeof(IServiceProviderIsKeyedService)) return this;
        if (serviceType == typeof(IKeyedServiceProvider)) return this;
        if (serviceType == typeof(IServiceScopeFactory)) return this;

        // Honor the Microsoft.Extensions.DependencyInjection contract for collection resolution:
        // a request for IEnumerable<T> (the shape GetServices<T>() uses) returns an empty array
        // when nothing is registered, rather than attempting to activate the interface itself.
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return Array.CreateInstance(serviceType.GetGenericArguments()[0], 0);
        }

        return Activator.CreateInstance(serviceType);
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType) => true;

    /// <inheritdoc/>
    public bool IsKeyedService(Type serviceType, object? serviceKey) => true;

    /// <inheritdoc/>
    public object? GetKeyedService(Type serviceType, object? serviceKey) => CreateKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        CreateKeyedService(serviceType, serviceKey)
            ?? throw new InvalidOperationException($"No service for type '{serviceType.FullName}' with key '{serviceKey}' has been registered.");

    static object? CreateKeyedService(Type serviceType, object? serviceKey)
    {
        // The chronicle client requests IActivitySource<T> keyed by ClientActivity.SourceName when
        // the host did not register it explicitly. Synthesize a working instance backed by a
        // System.Diagnostics.ActivitySource named after the key so client-side tracing degrades to
        // a no-op rather than throwing. Other keyed lookups fall back to default-constructor
        // activation, matching the unkeyed behavior.
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IActivitySource<>))
        {
            var sourceName = serviceKey as string ?? serviceType.GetGenericArguments()[0].FullName ?? serviceType.GetGenericArguments()[0].Name;
            var concrete = typeof(ActivitySource<>).MakeGenericType(serviceType.GetGenericArguments());
            return Activator.CreateInstance(concrete, new System.Diagnostics.ActivitySource(sourceName));
        }

        return Activator.CreateInstance(serviceType);
    }
}
