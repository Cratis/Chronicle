// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Grains;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Execution;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the default Cratis event log.
    /// </summary>
    public class MongoDBProjectionEventProvider : IProjectionEventProvider
    {
        readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
        readonly IExecutionContextManager _executionContextManager;
        readonly ProviderFor<IProjectionPositions> _positionsProvider;
        readonly ISchemaStore _schemaStore;
        readonly IJsonComplianceManager _jsonComplianceManager;
        readonly IClusterClient _clusterClient;

        /// <inheritdoc/>
        public ProjectionEventProviderTypeId TypeId => "c0c0196f-57e3-4860-9e3b-9823cf45df30";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionEventProvider"/> class.
        /// </summary>
        /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
        /// <param name="positionsProvider">Provider for <see cref="IProjectionPositions"/>.</param>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        /// <param name="clusterClient"><see cref="IClusterClient"/> for working with the Orleans cluster.</param>
        public MongoDBProjectionEventProvider(
            ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
            IExecutionContextManager executionContextManager,
            ProviderFor<IProjectionPositions> positionsProvider,
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager,
            IClusterClient clusterClient)
        {
            _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
            _executionContextManager = executionContextManager;
            _positionsProvider = positionsProvider;
            _schemaStore = schemaStore;
            _jsonComplianceManager = jsonComplianceManager;
            _clusterClient = clusterClient;
        }

        /// <inheritdoc/>
        public async Task<IEventCursor> GetFromPosition(IProjection projection, EventLogSequenceNumber start)
        {
            if (!projection.EventTypes.Any())
            {
                return new EventCursor(_schemaStore, _jsonComplianceManager, null);
            }

            var collection = _eventStoreDatabaseProvider().GetEventLogCollectionFor(EventLogId.Default);

            var offsetFilter = Builders<Store.MongoDB.Event>.Filter.Gte(_ => _.SequenceNumber, start);
            var eventTypeFilters = projection.EventTypes.Select(_ =>
                                        Builders<Store.MongoDB.Event>.Filter.Eq(_ => _.Type, _.Id)).ToArray() ?? Array.Empty<FilterDefinition<Store.MongoDB.Event>>();

            var filter = Builders<Store.MongoDB.Event>.Filter.And(
                offsetFilter,
                Builders<Store.MongoDB.Event>.Filter.Or(eventTypeFilters));

            var cursor = await collection.FindAsync(
                filter,
                new()
                {
                    Sort = Builders<Store.MongoDB.Event>.Sort.Ascending(_ => _.SequenceNumber)
                });

            return new EventCursor(_schemaStore, _jsonComplianceManager, cursor);
        }

        /// <inheritdoc/>
        public async Task ProvideFor(IProjectionPipeline pipeline, ISubject<Event> subject)
        {
            foreach (var resultStore in pipeline.ResultStores)
            {
                var currentOffset = await _positionsProvider().GetFor(pipeline.Projection, resultStore.Key);
                var streamProvider = _clusterClient.GetStreamProvider("event-log");
                var tenantId = _executionContextManager.Current.TenantId;
                var stream = streamProvider.GetStream<AppendedEvent>(EventLogId.Default, tenantId.ToString());
                await stream.SubscribeAsync(
                    async (@event, _) =>
                    {
                        _executionContextManager.Establish(tenantId, CorrelationId.New());
                        var eventSchema = await _schemaStore.GetFor(@event.Metadata.EventType.Id, @event.Metadata.EventType.Generation);
                        var releasedContent = await _jsonComplianceManager.Release(eventSchema.Schema, @event.EventContext.EventSourceId, JObject.Parse(@event.Content));

                        var content = BsonSerializer.Deserialize<ExpandoObject>(releasedContent.ToString());
                        subject.OnNext(new(
                            @event.Metadata.SequenceNumber,
                            @event.Metadata.EventType,
                            @event.EventContext.Occurred,
                            @event.EventContext.EventSourceId,
                            content
                        ));
                    },
                    new EventLogSequenceNumberTokenWithFilter(currentOffset, pipeline.Projection.EventTypes.ToArray()));
            }
        }
    }
}
