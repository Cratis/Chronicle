// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the implementation of <see cref="IProjectionManager"/>.
/// </summary>
[Singleton]
public class ProjectionManager : IProjectionManager
{
    readonly ConcurrentDictionary<string, IProjection> _projections = new();

    /// <inheritdoc/>
    public void Register(EventStoreName eventStore, EventStoreNamespaceName @namespace, IProjection projection)
    {
        var key = KeyHelper.Combine(eventStore, @namespace, projection.Identifier);
        _projections[key] = projection;
    }

    /// <inheritdoc/>
    public bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection) =>
        _projections.TryGetValue(KeyHelper.Combine(eventStore, @namespace, id), out projection);
}
