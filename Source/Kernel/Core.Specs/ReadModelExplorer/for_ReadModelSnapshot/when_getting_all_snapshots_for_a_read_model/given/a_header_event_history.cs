// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model.given;

public class a_header_event_history : a_projection_with_a_history
{
    protected AppendedEvent[] _stored;
    protected readonly List<AppendedEvent> _projected = [];
    protected IEnumerable<ReadModelSnapshot> _result;
    protected virtual EventGenerationDelivery Delivery => EventGenerationDelivery.Compatibility;

    async Task Establish()
    {
        _stored = header_events.All();
        var definition = await _projection.GetDefinition();
        _projection.GetDefinition().Returns(definition with { GenerationDelivery = Delivery });
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(_stored);
        _storage.GetEventStore("test-store").GetNamespace("test-namespace").GetEventSequence("event-log")
            .GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(cursor);
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(_ => Task.FromResult(_stored));
        _projection.ProcessForSingleReadModel(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _projected.AddRange(events);
                dynamic state = new ExpandoObject();
                state.copied = JsonSerializer.Serialize(events);
                return Task.FromResult((ExpandoObject)state);
            });
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => JsonSerializer.SerializeToNode(call.Arg<ExpandoObject>())!.AsObject());
    }
}
