// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;
using Aksio.Types;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class Sinks : ISinks
{
    readonly IDictionary<SinkTypeId, ISinkFactory> _factories;
    readonly ConcurrentDictionary<SinkKey, ISink> _stores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Sinks"/> class.
    /// </summary>
    /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
    public Sinks(IInstancesOf<ISinkFactory> stores)
    {
        _factories = stores.ToDictionary(_ => _.TypeId, _ => _);
    }

    /// <inheritdoc/>
    public ISink GetForTypeAndModel(SinkTypeId typeId, Model model)
    {
        ThrowIfUnknownProjectionResultStore(typeId);
        var key = new SinkKey(typeId, model);
        if (_stores.TryGetValue(key, out var store)) return store;
        return _stores[key] = _factories[typeId].CreateFor(model);
    }

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    void ThrowIfUnknownProjectionResultStore(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkKey(SinkTypeId TypeId, Model Model);
}
