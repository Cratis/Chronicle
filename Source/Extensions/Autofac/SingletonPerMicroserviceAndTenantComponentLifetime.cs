// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;
using Autofac.Core;

namespace Aksio.Cratis.Extensions.Autofac;

/// <summary>
/// Represents an implementation of <see cref="IComponentLifetime"/> for scoping per microservice and tenant.
/// </summary>
public class SingletonPerMicroserviceAndTenantComponentLifetime : IComponentLifetime
{
    /// <summary>
    /// Gets the global instance.
    /// </summary>
    public static readonly SingletonPerMicroserviceAndTenantComponentLifetime Instance = new();

    readonly ConcurrentDictionary<MicroserviceAndTenant, SingletonPerMicroserviceAndTenantLifetimeScope> _scopes = new();

    /// <inheritdoc/>
    public ISharingLifetimeScope FindScope(ISharingLifetimeScope mostNestedVisibleScope)
    {
        var context = ExecutionContextManager.GetCurrent();
        var key = new MicroserviceAndTenant(context.MicroserviceId, context.TenantId);
        if (!_scopes.ContainsKey(key))
        {
            _scopes[key] = new SingletonPerMicroserviceAndTenantLifetimeScope(mostNestedVisibleScope.RootLifetimeScope, mostNestedVisibleScope);
        }

        return _scopes[key];
    }
}
