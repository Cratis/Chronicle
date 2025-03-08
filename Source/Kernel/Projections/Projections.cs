// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
[Singleton]
public class Projections(IProjectionFactory projectionFactory) : IProjections
{
    readonly ConcurrentDictionary<EventStoreName, ProjectionDefinition> _definitions = new();
    readonly ConcurrentDictionary<string, IProjection> _projections = new();

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var definition in definitions)
        {
            _definitions[eventStore] = definition;
            foreach (var @namespace in namespaces)
            {
                var projection = await projectionFactory.Create(eventStore, @namespace, definition);
                var key = KeyHelper.Combine(eventStore, @namespace, definition.Identifier);
                _projections[key] = projection;
            }
        }
    }

    /// <inheritdoc/>
    public async Task AddNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        foreach (var definition in _definitions.Values)
        {
            var key = KeyHelper.Combine(eventStore, @namespace, definition.Identifier);
            if (!_projections.ContainsKey(key))
            {
                _projections[key] = await projectionFactory.Create(eventStore, @namespace, definition);
            }
        }
    }

    /// <inheritdoc/>
    public bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection) =>
        _projections.TryGetValue(KeyHelper.Combine(eventStore, @namespace, id), out projection);
}
