// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grains.Observation.Reducers;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespace"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStoreNamespace"/> class.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
/// <param name="name">The <see cref="EventStoreNamespaceName"/>.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
public class EventStoreNamespace(
    IStorage storage,
    EventStoreName eventStore,
    EventStoreNamespaceName name,
    IObjectComparer objectComparer,
    IInstancesOf<ISinkFactory> sinkFactories) : IEventStoreNamespace
{
    /// <inheritdoc/>
    public EventStoreNamespaceName Name { get; } = name;

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage Storage { get; } = storage.GetEventStore(eventStore).GetNamespace(name);

    /// <inheritdoc/>
    public IReducerPipelineFactory ReducerPipelines { get; } = new ReducerPipelineFactory(
            storage,
            objectComparer);

    /// <inheritdoc/>
    public ISinks Sinks { get; } = new Storage.Sinks.Sinks(eventStore, name, sinkFactories);
}
