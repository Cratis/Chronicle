// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/>.
/// </summary>
public class ProjectionSinks : IProjectionSinks
{
    record Key(ProjectionSinkTypeId TypeId, Model Model);

    readonly IDictionary<ProjectionSinkTypeId, IProjectionSinkFactory> _factories;
    readonly ConcurrentDictionary<Key, IProjectionSink> _stores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSinks"/> class.
    /// </summary>
    /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="IProjectionSinkFactory"/>.</param>
    public ProjectionSinks(IInstancesOf<IProjectionSinkFactory> stores)
    {
        _factories = stores.ToDictionary(_ => _.TypeId, _ => _);
    }

    /// <inheritdoc/>
    public IProjectionSink GetForTypeAndModel(ProjectionSinkTypeId typeId, Model model)
    {
        ThrowIfUnknownProjectionResultStore(typeId);
        var key = new Key(typeId, model);
        if (_stores.ContainsKey(key)) return _stores[key];
        return _stores[key] = _factories[typeId].CreateFor(model);
    }

    /// <inheritdoc/>
    public bool HasType(ProjectionSinkTypeId typeId) => _factories.ContainsKey(typeId);

    void ThrowIfUnknownProjectionResultStore(ProjectionSinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownProjectionSink(typeId);
    }
}
