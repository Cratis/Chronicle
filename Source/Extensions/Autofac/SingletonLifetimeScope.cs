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
/// Represents an implementation of <see cref="ISharingLifetimeScope"/> used in different singleton scenarios.
/// </summary>
/// <typeparam name="TComponentLifetime">Type of <see cref="IComponentLifetime"/>.</typeparam>
public class SingletonLifetimeScope<TComponentLifetime> : ISharingLifetimeScope
    where TComponentLifetime : IComponentLifetime
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
    /// Initializes a new instance of the <see cref="SingletonLifetimeScope{T}"/> class.
    /// </summary>
    /// <param name="parentLifetimeScope">The parent lifetime scope.</param>
    public SingletonLifetimeScope(ISharingLifetimeScope parentLifetimeScope)
    {
        RootLifetimeScope = this;
        ParentLifetimeScope = parentLifetimeScope;
        ComponentRegistry = parentLifetimeScope.ComponentRegistry;
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
        if (registration.Lifetime is not TComponentLifetime)
        {
            return ParentLifetimeScope!.CreateSharedInstance(primaryId, qualifyingId, creator);
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
