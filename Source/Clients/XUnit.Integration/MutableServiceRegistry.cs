// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// A mutable registry of per-test service factories that allows the shared DI container
/// to serve different service instances for each test run without rebuilding the container.
/// </summary>
/// <remarks>
/// The registry is registered as a singleton in DI. For each type captured from a test
/// fixture's <c>ConfigureServices</c> override, a transient delegate is added to the DI
/// container that delegates to this registry. Before each test the registry is updated
/// with the new fixture's service instances so that subsequent DI resolutions return the
/// correct objects for the current test.
/// </remarks>
internal class MutableServiceRegistry
{
    readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = [];

    /// <summary>
    /// Gets the service types currently registered in the registry.
    /// </summary>
    public IEnumerable<Type> RegisteredTypes => [.. _factories.Keys];

    /// <summary>
    /// Updates (or adds) factories for each service descriptor in <paramref name="descriptors"/>.
    /// </summary>
    /// <param name="descriptors">The <see cref="ServiceDescriptor"/> instances to update from.</param>
    public void Update(IEnumerable<ServiceDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (descriptor.ImplementationInstance is not null)
            {
                var instance = descriptor.ImplementationInstance;
                _factories[descriptor.ServiceType] = _ => instance;
            }
            else if (descriptor.ImplementationFactory is not null)
            {
                _factories[descriptor.ServiceType] = descriptor.ImplementationFactory;
            }
        }
    }

    /// <summary>
    /// Returns the service instance for <paramref name="serviceType"/> using
    /// <paramref name="serviceProvider"/> to satisfy any factory dependencies.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to pass to factory lambdas.</param>
    /// <returns>The resolved service instance.</returns>
    public object Get(Type serviceType, IServiceProvider serviceProvider) =>
        _factories[serviceType](serviceProvider);
}
