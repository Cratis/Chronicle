// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Models;
using Aksio.Cratis.Sinks;
using Aksio.Types;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class Sinks : ISinks
{
    readonly IDictionary<SinkTypeId, ISinkFactory> _factories;
    readonly ConcurrentDictionary<SinkKey, ISink> _sinks = new();
    readonly EventStoreName _eventStoreName;
    readonly EventStoreNamespaceName _eventStoreNamespaceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sinks"/> class.
    /// </summary>
    /// <param name="eventStoreName"><see cref="EventStoreName"/> the <see cref="Sinks"/> are for.</param>
    /// <param name="eventStoreNamespaceName"><see cref="EventStoreNamespaceName"/> the <see cref="Sinks"/> are for.</param>
    /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
    public Sinks(
        EventStoreName eventStoreName,
        EventStoreNamespaceName eventStoreNamespaceName,
        IInstancesOf<ISinkFactory> stores)
    {
        _factories = stores.ToDictionary(_ => _.TypeId, _ => _);
        _eventStoreName = eventStoreName;
        _eventStoreNamespaceName = eventStoreNamespaceName;
    }

    /// <inheritdoc/>
    public ISink GetFor(
        SinkTypeId typeId,
        Model model)
    {
        ThrowIfUnknownProjectionResultStore(typeId);
        var key = new SinkKey(typeId, model);
        if (_sinks.TryGetValue(key, out var store)) return store;
        return _sinks[key] = _factories[typeId].CreateFor(_eventStoreName, _eventStoreNamespaceName, model);
    }

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    void ThrowIfUnknownProjectionResultStore(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkKey(SinkTypeId TypeId, Model Model);
}
