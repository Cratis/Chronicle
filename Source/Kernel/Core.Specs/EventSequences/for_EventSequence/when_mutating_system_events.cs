// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_mutating_system_events : given.an_event_sequence
{
    Exception[] _errors;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceKey = new EventSequenceKey(SequenceId, EventStoreName.System, EventStoreNamespace);
        return await base.CreateEventSequence();
    }

    void Establish()
    {
        _eventSequenceStorage.ClearReceivedCalls();
        _identityStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        _errors =
        [
            await Catch.Exception(() => _eventSequence.Revise(0UL, _eventType, new(), CorrelationId.New(), [], Identity.System)),
            await Catch.Exception(() => _eventSequence.Redact(0UL, "spec", CorrelationId.New(), [], Identity.System)),
            await Catch.Exception(() => _eventSequence.Redact(_eventSourceId, "spec", [], CorrelationId.New(), [], Identity.System))
        ];
    }

    [Fact] void should_reject_all_three_mutations() => _errors.ShouldEachConformTo(error => error is SystemEventStoreEventsAreImmutable);
    [Fact] void should_not_touch_storage() => _eventSequenceStorage.ReceivedCalls().Any(call => new[] { "Redact", "Revise", "GetEventAt" }.Contains(call.GetMethodInfo().Name)).ShouldBeFalse();
    [Fact] void should_not_resolve_identities() => _identityStorage.ReceivedCalls().ShouldBeEmpty();
}
