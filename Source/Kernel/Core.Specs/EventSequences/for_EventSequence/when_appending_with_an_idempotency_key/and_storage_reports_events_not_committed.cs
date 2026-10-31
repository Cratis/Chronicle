// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_storage_reports_events_not_committed : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
    {
        _writes++;
        if (_writes == 1) throw new EventsNotCommitted(new Exception("not committed"));
        return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>()));
    });
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_retry_once() => _writes.ShouldEqual(2);
    [Fact] void should_append() => _result.Outcome.ShouldEqual(AppendOutcome.Committed);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
