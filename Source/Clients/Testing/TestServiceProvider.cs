// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a minimal dictionary-backed <see cref="IServiceProvider"/> for grain construction in test scenarios.
/// </summary>
internal sealed class TestServiceProvider : IServiceProvider
{
    readonly Dictionary<Type, object> _services = [];

    /// <inheritdoc/>
    public object? GetService(Type serviceType) =>
        _services.GetValueOrDefault(serviceType);

    /// <summary>
    /// Registers a service instance for the specified type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    internal void AddService<T>(T instance)
        where T : class =>
        _services[typeof(T)] = instance;
}
