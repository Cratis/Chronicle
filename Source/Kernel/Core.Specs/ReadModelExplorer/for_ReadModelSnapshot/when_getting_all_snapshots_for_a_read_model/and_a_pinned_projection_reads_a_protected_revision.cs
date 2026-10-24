// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_a_pinned_projection_reads_a_protected_revision : given.a_projection_with_a_history
{
    AppendedEvent _stored;
    AppendedEvent _projected;
    AppendedEvent[] _displayInputs;
    IEnumerable<ReadModelSnapshot> _result;

    async Task Establish()
    {
        var definition = await _projection.GetDefinition();
        _projection.GetDefinition().Returns(definition with { GenerationDelivery = EventGenerationDelivery.Pinned });
        dynamic content = new ExpandoObject();
        content.name = "protected-content";
        _stored = new AppendedEvent(Concepts.Events.EventContext.EmptyWithEventSourceId("my-instance") with
        {
            EventType = new("my-event", 1),
            SequenceNumber = 1,
            Occurred = DateTimeOffset.UnixEpoch,
            CorrelationId = FirstCorrelation
        }, (ExpandoObject)content)
        {
            RevisedGeneration = 1,
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"protected-revision\"}" }
        };
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([_stored]);
        _storage.GetEventStore("test-store").GetNamespace("test-namespace").GetEventSequence("event-log")
            .GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        _projection.ProcessForSingleReadModel(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _projected = call.ArgAt<IEnumerable<AppendedEvent>>(2).Single();
                return Task.FromResult(call.ArgAt<ExpandoObject>(1));
            });
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call =>
            {
                _displayInputs = call.ArgAt<IEnumerable<AppendedEvent>>(0).ToArray();
                dynamic released = new ExpandoObject();
                released.name = "released-for-display";
                return Task.FromResult(_displayInputs.Select(@event => @event with { Content = (ExpandoObject)released }).ToArray());
            });
    }

    async Task Because() => _result = await AllSnapshots();

    [Fact] void should_pass_the_protected_revision_to_the_projection() => _projected.ShouldBeSame(_stored);
    [Fact] void should_not_feed_decrypted_display_content_back_to_generation_release() =>
        ((IDictionary<string, object?>)_projected.Content)["name"].ShouldEqual("protected-content");
    [Fact] void should_release_the_original_stored_event_for_display() => _displayInputs.Single().ShouldBeSame(_stored);
    [Fact] void should_release_the_display_once() => _eventCompliance.Received(1)
        .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>());
    [Fact] void should_display_decrypted_stored_content() => _result.Single().Events.Single().Content.ShouldEqual("{\"name\":\"released-for-display\"}");
}
