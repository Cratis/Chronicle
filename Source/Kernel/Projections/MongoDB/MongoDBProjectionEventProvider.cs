// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.EventLogs;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Execution;
using MongoDB.Driver;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IProjectionEventProvider"/> for the default Cratis event log.
/// </summary>
public class MongoDBProjectionEventProvider : IProjectionEventProvider
{
    readonly IEventLogStorageProvider _eventLogStorageProvider;
    readonly IEventConverter _converter;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IProjectionPositions> _positionsProvider;
    readonly ISchemaStore _schemaStore;
    readonly IClusterClient _clusterClient;
    readonly ConcurrentDictionary<IProjectionPipeline, StreamSubscriptionHandle<AppendedEvent>> _subscriptionsPerPipeline = new();

    /// <inheritdoc/>
    public ProjectionEventProviderTypeId TypeId => "c0c0196f-57e3-4860-9e3b-9823cf45df30";

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBProjectionEventProvider"/> class.
    /// </summary>
    /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
    /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="positionsProvider">Provider for <see cref="IProjectionPositions"/>.</param>
    /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> for working with the Orleans cluster.</param>
    public MongoDBProjectionEventProvider(
        IEventLogStorageProvider eventLogStorageProvider,
        IEventConverter converter,
        IExecutionContextManager executionContextManager,
        ProviderFor<IProjectionPositions> positionsProvider,
        ISchemaStore schemaStore,
        IClusterClient clusterClient)
    {
        _eventLogStorageProvider = eventLogStorageProvider;
        _converter = converter;
        _executionContextManager = executionContextManager;
        _positionsProvider = positionsProvider;
        _schemaStore = schemaStore;
        _clusterClient = clusterClient;
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> GetLastInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId) => _eventLogStorageProvider.GetLastInstanceFor(eventTypeId, eventSourceId);

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(IProjection projection, EventLogSequenceNumber sequenceNumber)
    {
        if (!projection.EventTypes.Any())
        {
            return new EventCursor(_converter, null);
        }

        return await _eventLogStorageProvider.GetFromSequenceNumber(sequenceNumber, eventTypes: projection.EventTypes);
    }

    /// <inheritdoc/>
    public async Task ProvideFor(IProjectionPipeline pipeline, ISubject<AppendedEvent> subject)
    {
        foreach (var resultStore in pipeline.ResultStores)
        {
            var currentOffset = await _positionsProvider().GetFor(pipeline.Projection, resultStore.Key);
            var streamProvider = _clusterClient.GetStreamProvider("event-log");
            var tenantId = _executionContextManager.Current.TenantId;
            var stream = streamProvider.GetStream<AppendedEvent>(EventLogId.Default, tenantId.ToString());
            _subscriptionsPerPipeline[pipeline] = await stream.SubscribeAsync(
                async (@event, _) =>
                {
                    _executionContextManager.Establish(tenantId, CorrelationId.New());
                    var eventSchema = await _schemaStore.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                    subject.OnNext(new(@event.Metadata, @event.Context, @event.Content));
                },
                new EventLogSequenceNumberTokenWithFilter(currentOffset, pipeline.Projection.EventTypes.ToArray()));
        }
    }

    /// <inheritdoc/>
    public async Task StopProvidingFor(IProjectionPipeline pipeline)
    {
        if (_subscriptionsPerPipeline.ContainsKey(pipeline))
        {
            await _subscriptionsPerPipeline[pipeline].UnsubscribeAsync();
            _subscriptionsPerPipeline.Remove(pipeline, out _);
        }
    }
}
