// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly IClusterClient _clusterClient;

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox Outbox { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with execution context.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> in the system.</param>
    public EventStore(
        IEventSerializer serializer,
        IClusterClient clusterClient,
        IExecutionContextManager executionContextManager,
        IEventTypes eventTypes)
    {
        _clusterClient = clusterClient;

        var eventLog = _clusterClient.GetGrain<Store.Grains.IEventSequence>(
            EventSequenceId.Log,
            keyExtension: executionContextManager.Current.ToMicroserviceAndTenant());
        EventLog = new EventLog(eventTypes, serializer, eventLog, clusterClient, executionContextManager);

        var eventOutbox = _clusterClient.GetGrain<Store.Grains.IEventSequence>(
            EventSequenceId.Outbox,
            keyExtension: executionContextManager.Current.ToMicroserviceAndTenant());
        Outbox = new EventOutbox(eventTypes, serializer, eventOutbox);
    }
}
