// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_the_event_is_already_redacted : given.a_content_mutation_with_observers
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
                new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First),
                EventSourceType.Default,
                _eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                42UL,
                CorrelationId.New()),
            new ExpandoObject()));

    Task Because() => _eventSequence.Redact(42UL, "reason", CorrelationId.New(), [], Identity.System);

    [Fact] void should_leave_the_epoch_at_its_start_value_after_settling() => _stateStorage.State.MutationEpoch.IsMutating.ShouldBeFalse();
}
