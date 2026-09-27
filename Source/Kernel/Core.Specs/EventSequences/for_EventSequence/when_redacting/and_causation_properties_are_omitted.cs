// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_causation_properties_are_omitted : given.an_event_sequence_omitting_causation_properties
{
    void Establish() => _eventSequenceStorage.Redact(
        Arg.Any<EventSequenceNumber>(),
        Arg.Any<RedactionReason>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<IEnumerable<IdentityId>>(),
        Arg.Any<DateTimeOffset>())
        .Returns(new AppendedEvent(
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
        await _eventSequence.Redact(0UL, RedactionReason.Unknown, CorrelationId.New(), _requestedCausation, Identity.System);
        _storedCausation = StoredFor("Redact", 3);
    }

    [Fact] void should_omit_the_operations_causation_properties() => _storedCausation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_type_and_time() => _storedCausation.Select(causation => (causation.Type, causation.Occurred)).ShouldEqual(_requestedCausation.Select(causation => (causation.Type, causation.Occurred)));
    [Fact] void should_leave_the_request_unchanged() => _requestedCausation[0].Properties["name"].ShouldEqual("personal value");
}
