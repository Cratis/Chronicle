// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Resolving;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Represents an implementation of <see cref="ISharingLifetimeScope"/>.
/// </summary>
public class SingletonPerMicroserviceAndTenantLifetimeScope : ISharingLifetimeScope
{
    readonly ConcurrentDictionary<Guid, object> _instances = new();

    /// <inheritdoc/>
    public ISharingLifetimeScope RootLifetimeScope { get; }

    /// <inheritdoc/>
    public ISharingLifetimeScope? ParentLifetimeScope { get; }

    /// <inheritdoc/>
    public IDisposer Disposer => throw new NotImplementedException();

    /// <inheritdoc/>
    public object Tag => throw new NotImplementedException();

    /// <inheritdoc/>
    public IComponentRegistry ComponentRegistry { get; }

    /// <inheritdoc/>
    public event EventHandler<LifetimeScopeBeginningEventArgs> ChildLifetimeScopeBeginning = (s, e) => { };

    /// <inheritdoc/>
    public event EventHandler<LifetimeScopeEndingEventArgs> CurrentScopeEnding = (s, e) => { };

    /// <inheritdoc/>
    public event EventHandler<ResolveOperationBeginningEventArgs> ResolveOperationBeginning = (s, e) => { };

    /// <summary>
    /// Initializes a new instance of the <see cref="SingletonPerMicroserviceAndTenantLifetimeScope"/> class.
    /// </summary>
    /// <param name="rootLifetimeScope">The root lifetime scope.</param>
    /// <param name="parentLifetimeScope">The parent lifetime scope.</param>
    public SingletonPerMicroserviceAndTenantLifetimeScope(ISharingLifetimeScope rootLifetimeScope, ISharingLifetimeScope parentLifetimeScope)
    {
        RootLifetimeScope = rootLifetimeScope;
        ParentLifetimeScope = parentLifetimeScope;
        ComponentRegistry = rootLifetimeScope.ComponentRegistry;
    }

    /// <inheritdoc/>
    public ILifetimeScope BeginLifetimeScope() => throw new NotImplementedException();

    /// <inheritdoc/>
    public ILifetimeScope BeginLifetimeScope(object tag) => throw new NotImplementedException();

    /// <inheritdoc/>
    public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction) => throw new NotImplementedException();

    /// <inheritdoc/>
    public ILifetimeScope BeginLifetimeScope(object tag, Action<ContainerBuilder> configurationAction) => throw new NotImplementedException();

    /// <inheritdoc/>
    public object CreateSharedInstance(Guid id, Func<object> creator) => _instances[id] = creator();

    /// <inheritdoc/>
    public object CreateSharedInstance(Guid primaryId, Guid? qualifyingId, Func<object> creator)
    {
        var registration = ComponentRegistry.Registrations.Single(_ => _.Id == primaryId);
        if (registration.Lifetime is not SingletonPerMicroserviceAndTenantComponentLifetime)
        {
            return RootLifetimeScope.CreateSharedInstance(primaryId, qualifyingId, creator);
        }

        return _instances[primaryId] = creator();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public object ResolveComponent(ResolveRequest request)
    {
        return null!;
    }

    /// <inheritdoc/>
    public bool TryGetSharedInstance(Guid id, [NotNullWhen(true)] out object? value)
    {
        return _instances.TryGetValue(id, out value);
    }

    /// <inheritdoc/>
    public bool TryGetSharedInstance(Guid primaryId, Guid? qualifyingId, [NotNullWhen(true)] out object? value)
    {
        return _instances.TryGetValue(primaryId, out value);
    }
}
