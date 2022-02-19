// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;
using Orleans;
using EngineProjection = Aksio.Cratis.Events.Projections.IProjection;

namespace Aksio.Cratis.Events.Projections.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjection"/>.
    /// </summary>
    public class Projection : Grain, IProjection
    {
        readonly IProjectionDefinitions _projectionDefinitions;
        readonly IProjectionFactory _projectionFactory;
        readonly IObjectsComparer _objectsComparer;
        readonly IEventLogStorageProvider _eventLogStorageProvider;
        EngineProjection? _projection;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="projectionDefinitions"><see cref="IProjectionDefinitions"/>.</param>
        /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
        /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
        /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
        public Projection(
            IProjectionDefinitions projectionDefinitions,
            IProjectionFactory projectionFactory,
            IObjectsComparer objectsComparer,
            IEventLogStorageProvider eventLogStorageProvider)
        {
            _projectionDefinitions = projectionDefinitions;
            _projectionFactory = projectionFactory;
            _objectsComparer = objectsComparer;
            _eventLogStorageProvider = eventLogStorageProvider;
        }

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            var projectionId = this.GetPrimaryKey();
            var definition = await _projectionDefinitions.GetFor(projectionId);
            _projection = await _projectionFactory.CreateFrom(definition);
        }

        /// <inheritdoc/>
        public async Task<JsonObject> GetModelInstanceById(EventSourceId eventSourceId)
        {
            if (_projection is null)
            {
                return new JsonObject();
            }
            var cursor = await _eventLogStorageProvider.GetFromSequenceNumber(EventLogSequenceNumber.First, eventSourceId, _projection.EventTypes);
            var state = new ExpandoObject();
            while (await cursor.MoveNext())
            {
                if (!cursor.Current.Any())
                {
                    break;
                }

                foreach (var @event in cursor.Current)
                {
                    var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, state);
                    var context = new ProjectionEventContext(new Key(@event.Context.EventSourceId, ArrayIndexers.NoIndexers), @event, changeset);
                    _projection.OnNext(context);

                    foreach (var change in changeset.Changes)
                    {
                        state = state.OverwriteWith((change.State as ExpandoObject)!);
                    }
                }
            }

            // TODO: Conversion from ExpandoObject to JsonObject can be improved - they're effectively both just Dictionary<string, object>
            var json = JsonSerializer.Serialize(state);
            var jsonObject = JsonNode.Parse(json)!;
            return (jsonObject as JsonObject)!;
        }
    }
}
