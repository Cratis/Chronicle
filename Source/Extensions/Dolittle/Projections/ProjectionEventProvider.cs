// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Events;
using Cratis.Events.Projections;
using Cratis.Events.Projections.Pipelines;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using IEventStore = Cratis.Extensions.Dolittle.EventStore.IEventStore;
using IEventStream = Cratis.Extensions.Dolittle.EventStore.IEventStream;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the Dolittle event store.
    /// </summary>
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        readonly IEventStream _eventStream;
        readonly ILogger<ProjectionEventProvider> _logger;
        readonly ConcurrentDictionary<IProjectionPipeline, ConcurrentBag<ISubject<Event>>> _piplinesWithSubjects = new();

        /// <summary>
        /// Gets the unique identifier of the provider.
        /// </summary>
        public static readonly ProjectionEventProviderTypeId ProjectionEventProviderTypeId = "965f8ad2-38d7-4d49-a3c5-b67dbaa17781";

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectionEventProvider"/>.
        /// </summary>
        /// <param name="eventStore">The Dolittle <see cref="IEventStore"/>.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionEventProvider(
            IEventStore eventStore,
            ILogger<ProjectionEventProvider> logger)
        {
            _eventStream = eventStore.GetStream(EventStore.EventStreamId.EventLog);
            _logger = logger;
            WatchForEvents();
        }

        /// <inheritdoc/>
        public ProjectionEventProviderTypeId TypeId => ProjectionEventProviderTypeId;

        /// <inheritdoc/>
        public void ProvideFor(IProjectionPipeline pipeline, ISubject<Event> subject)
        {
            _logger.ProvidingFor(pipeline.Projection.Identifier);
            if (!_piplinesWithSubjects.ContainsKey(pipeline))
            {
                _piplinesWithSubjects[pipeline] = new();
            }

            _piplinesWithSubjects[pipeline].Add(subject);
        }

        /// <inheritdoc/>
        public async Task<IEventCursor> GetFromPosition(IProjection projection, EventLogSequenceNumber start)
        {
            if (!projection.EventTypes.Any())
            {
                _logger.SkippingProvidingForProjectionDueToNoEventTypes(projection.Identifier);
                return new EventCursor(null);
            }

            var eventTypes = projection.EventTypes.Select(_ => new global::Dolittle.SDK.Events.EventType(_.Id.Value)).ToArray();
            var cursor = await _eventStream.GetFromPosition(start, eventTypes);
            return new EventCursor(cursor);
        }

        void WatchForEvents()
        {
            Task.Run(() =>
            {
                var cursor = _eventStream.Watch();
                while (cursor.MoveNext())
                {
                    if (!cursor.Current.Any()) continue;

                    foreach (var (projectionPipeline, subjects) in _piplinesWithSubjects)
                    {
                        foreach (var subject in subjects)
                        {
                            foreach (var @event in cursor.Current.Select(_ => _.FullDocument.ToCratis()))
                            {
                                subject.OnNext(@event);
                            }
                        }
                    }
                }
            });
        }
    }
}
