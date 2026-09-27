// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_forwarded_event;

public class and_causation_properties_are_omitted : given.an_event_sequence_omitting_causation_properties
{
    EventContext _sourceContext;

    void Establish() => _sourceContext = EventContext.From(
        EventStore,
        EventStoreNamespace,
        _eventType,
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        0UL,
        CorrelationId.New()) with { Causation = _requestedCausation };

    async Task Because()
    {
        await _eventSequence.Append(
            _sourceContext.EventSourceType,
            _sourceContext.EventSourceId,
            _sourceContext.EventStreamType,
            _sourceContext.EventStreamId,
            _sourceContext.EventType,
            new JsonObject(),
            _sourceContext.CorrelationId,
            _sourceContext.Causation,
            Identity.System,
            [],
            ConcurrencyScope.None,
            subject: _sourceContext.Subject);
        _storedCausation = StoredAppend();
    }

    [Fact] void should_omit_forwarded_property_values() => _storedCausation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_forwarded_type_and_time() => _storedCausation.Select(causation => (causation.Type, causation.Occurred)).ShouldEqual(_requestedCausation.Select(causation => (causation.Type, causation.Occurred)));
    [Fact] void should_leave_source_causation_unchanged() => _sourceContext.Causation.First().Properties["name"].ShouldEqual("personal value");
}
