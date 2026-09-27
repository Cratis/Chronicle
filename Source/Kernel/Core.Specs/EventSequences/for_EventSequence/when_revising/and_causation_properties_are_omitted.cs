// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_causation_properties_are_omitted : given.an_event_sequence_omitting_causation_properties
{
    void Establish() => _eventSequenceStorage.GetEventAt(0UL).Returns(new AppendedEvent(
        EventContext.From(
            EventStore,
            EventStoreNamespace,
            _eventType,
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            0UL,
            CorrelationId.New()),
        new ExpandoObject()));

    async Task Because()
    {
        await _eventSequence.Revise(0UL, _eventType, new JsonObject(), CorrelationId.New(), _requestedCausation, Identity.System);
        _storedCausation = StoredFor("Revise", 3);
    }

    [Fact] void should_omit_property_values() => _storedCausation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_type_and_time() => _storedCausation.Select(causation => (causation.Type, causation.Occurred)).ShouldEqual(_requestedCausation.Select(causation => (causation.Type, causation.Occurred)));
    [Fact] void should_leave_the_request_unchanged() => _requestedCausation[0].Properties["name"].ShouldEqual("personal value");
}
