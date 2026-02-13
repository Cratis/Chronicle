// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sinks"/> class.
/// </remarks>
/// <param name="eventStoreName"><see cref="EventStoreName"/> the <see cref="Sinks"/> are for.</param>
/// <param name="eventStoreNamespaceName"><see cref="EventStoreNamespaceName"/> the <see cref="Sinks"/> are for.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
public class Sinks(
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespaceName,
    IInstancesOf<ISinkFactory> sinkFactories) : ISinks
{
    readonly Dictionary<SinkTypeId, ISinkFactory> _factories = sinkFactories.ToDictionary(_ => _.TypeId, _ => _);
    readonly ConcurrentDictionary<SinkKey, ISink> _sinks = new();

    /// <inheritdoc/>
    public ISink GetFor(ReadModelDefinition readModel)
    {
        ThrowIfUnknownSink(readModel.Sink.Type);
        var key = new SinkKey(readModel.Sink.Type, readModel.Sink.Configuration, readModel.ContainerName);
        if (_sinks.TryGetValue(key, out var store)) return store;
        return _sinks[key] = _factories[readModel.Sink.Type].CreateFor(eventStoreName, eventStoreNamespaceName, readModel);
    }

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    void ThrowIfUnknownSink(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkKey(SinkTypeId TypeId, SinkConfigurationId ConfigurationId, ReadModelContainerName ContainerName);
}
