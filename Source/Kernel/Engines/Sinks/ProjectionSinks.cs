// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;
using Aksio.Types;

namespace Aksio.Cratis.Kernel.Engines.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSinkFactory"/>.
/// </summary>
public class ProjectionSinks : IProjectionSinks
{
    sealed record Key(SinkTypeId TypeId, Model Model);

    readonly IDictionary<SinkTypeId, IProjectionSinkFactory> _factories;
    readonly ConcurrentDictionary<Key, ISink> _stores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSinks"/> class.
    /// </summary>
    /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="IProjectionSinkFactory"/>.</param>
    public ProjectionSinks(IInstancesOf<IProjectionSinkFactory> stores)
    {
        _factories = stores.ToDictionary(_ => _.TypeId, _ => _);
    }

    /// <inheritdoc/>
    public ISink GetForTypeAndModel(SinkTypeId typeId, Model model)
    {
        ThrowIfUnknownProjectionResultStore(typeId);
        var key = new Key(typeId, model);
        if (_stores.TryGetValue(key, out var store)) return store;
        return _stores[key] = _factories[typeId].CreateFor(model);
    }

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    void ThrowIfUnknownProjectionResultStore(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownProjectionSink(typeId);
    }
}
