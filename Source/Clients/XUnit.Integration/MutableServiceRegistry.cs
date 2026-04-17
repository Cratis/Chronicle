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
                _factories[descriptor.ServiceType] = _ => descriptor.ImplementationInstance;
            }
            else if (descriptor.ImplementationFactory is not null)
            {
                _factories[descriptor.ServiceType] = descriptor.ImplementationFactory;
            }
            else if (descriptor.ImplementationType is not null)
            {
                var implType = descriptor.ImplementationType;
                _factories[descriptor.ServiceType] = sp =>
                    ActivatorUtilities.CreateInstance(sp, implType);
            }
        }
    }

    /// <summary>
    /// Returns a value indicating whether the registry has a factory for <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="serviceType">The service type to check.</param>
    /// <returns><see langword="true"/> if the type has a registered factory; otherwise <see langword="false"/>.</returns>
    public bool HasService(Type serviceType) => _factories.ContainsKey(serviceType);

    /// <summary>
    /// Returns the service instance for <paramref name="serviceType"/> using
    /// <paramref name="serviceProvider"/> to satisfy any factory dependencies.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to pass to factory lambdas.</param>
    /// <returns>The resolved service instance.</returns>
    public object Get(Type serviceType, IServiceProvider serviceProvider) =>
        _factories[serviceType](serviceProvider);

    /// <summary>
    /// Tries to return the service instance for <paramref name="serviceType"/>.
    /// Returns <see langword="null"/> if the type is not registered.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to pass to factory lambdas.</param>
    /// <returns>The resolved service instance or <see langword="null"/>.</returns>
    public object? TryGet(Type serviceType, IServiceProvider serviceProvider) =>
        _factories.TryGetValue(serviceType, out var factory) ? factory(serviceProvider) : null;
}
