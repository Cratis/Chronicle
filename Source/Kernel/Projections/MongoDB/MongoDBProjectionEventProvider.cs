// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Compliance;
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

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the default Cratis event log.
    /// </summary>
    public class MongoDBProjectionEventProvider : IProjectionEventProvider
    {
        readonly IEventLogStorageProvider _eventLogStorageProvider;
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
        /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
        /// <param name="positionsProvider">Provider for <see cref="IProjectionPositions"/>.</param>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        /// <param name="clusterClient"><see cref="IClusterClient"/> for working with the Orleans cluster.</param>
        public MongoDBProjectionEventProvider(
            IEventLogStorageProvider eventLogStorageProvider,
            IExecutionContextManager executionContextManager,
            ProviderFor<IProjectionPositions> positionsProvider,
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager,
            IClusterClient clusterClient)
        {
            _eventLogStorageProvider = eventLogStorageProvider;
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

            return await _eventLogStorageProvider.GetFromSequenceNumber(start, eventTypes: projection.EventTypes);
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
                await stream.SubscribeAsync(
                    async (@event, _) =>
                    {
                        _executionContextManager.Establish(tenantId, CorrelationId.New());
                        var eventSchema = await _schemaStore.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                        subject.OnNext(new(@event.Metadata, @event.Context, @event.Content));
                    },
                    new EventLogSequenceNumberTokenWithFilter(currentOffset, pipeline.Projection.EventTypes.ToArray()));
            }
        }
    }
}
