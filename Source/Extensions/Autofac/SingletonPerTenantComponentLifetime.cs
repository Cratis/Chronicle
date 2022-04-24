// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Autofac.Core;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Represents an implementation of <see cref="IComponentLifetime"/> for scoping per tenant.
/// </summary>
public class SingletonPerTenantComponentLifetime : IComponentLifetime
{
    /// <summary>
    /// Gets the global instance.
    /// </summary>
    public static readonly SingletonPerMicroserviceAndTenantComponentLifetime Instance = new();

    readonly ConcurrentDictionary<TenantId, SingletonLifetimeScope<SingletonPerTenantComponentLifetime>> _scopes = new();

    /// <inheritdoc/>
    public ISharingLifetimeScope FindScope(ISharingLifetimeScope mostNestedVisibleScope)
    {
        var context = ExecutionContextManager.GetCurrent();
        if (!_scopes.ContainsKey(context.TenantId))
        {
            _scopes[context.TenantId] = new SingletonLifetimeScope<SingletonPerTenantComponentLifetime>(mostNestedVisibleScope.RootLifetimeScope, mostNestedVisibleScope);
        }

        return _scopes[context.TenantId];
    }
}
