// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Grains.Observation.Reducers;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespace"/>.
/// </summary>
public class EventStoreNamespace : IEventStoreNamespace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNamespace"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="name">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    public EventStoreNamespace(
        EventStoreName eventStore,
        EventStoreNamespaceName name,
        IEventStoreNamespaceStorage storage,
        IServiceProvider serviceProvider)
    {
        Name = name;
        Storage = storage;

        Sinks = new Storage.Sinks.Sinks(eventStore, name, serviceProvider.GetRequiredService<IInstancesOf<ISinkFactory>>());
        var sinks = new Storage.Sinks.Sinks(
            eventStore,
            name,
            serviceProvider.GetRequiredService<IInstancesOf<ISinkFactory>>());
        ReducerPipelines = new ReducerPipelines(
            sinks,
            serviceProvider.GetRequiredService<IObjectComparer>());
    }

    /// <inheritdoc/>
    public EventStoreNamespaceName Name { get; }

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage Storage { get; }

    /// <inheritdoc/>
    public IReducerPipelines ReducerPipelines { get; }

    /// <inheritdoc/>
    public ISinks Sinks { get; }
}
