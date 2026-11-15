// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_an_ungoverned_type_has_no_schema : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish()
    {
        _events.Insert(0, Event("Unregistered", 0));
        _eventTypes.GetFor((EventTypeId)"Unregistered", Arg.Any<EventTypeGeneration?>()).Returns(Task.FromException<EventTypeSchema>(new MissingEventSchemaForEventType("store", "Unregistered", EventTypeGeneration.First)));
    }

    async Task Because() => await Perform();

    [Fact] async Task should_request_only_the_governed_type() => await _eventsStorage.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(new[] { new EventType("Registered", EventTypeGeneration.First) })), cancellationToken: Arg.Any<CancellationToken>());
    [Fact] async Task should_index_the_governed_claims() => await _uniqueStorage.Received(2).Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), Arg.Any<Concepts.Events.Constraints.UniqueConstraintValue>(), "");
    [Fact] void should_complete() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_not_read_the_unregistered_schema() => await _eventTypes.DidNotReceive().GetFor((EventTypeId)"Unregistered", Arg.Any<EventTypeGeneration?>());
}
