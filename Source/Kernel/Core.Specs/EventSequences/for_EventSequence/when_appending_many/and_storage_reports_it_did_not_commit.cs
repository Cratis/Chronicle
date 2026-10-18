// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_storage_reports_it_did_not_commit : given.an_append_with_settlement
{
    void Establish() => _notCommitted = true;
    async Task Because() => _batchResult = await _eventSequence.AppendMany([ValidatedEvent().Event], CorrelationId.New(), [], Identity.System, new(new Dictionary<EventSourceId, Concepts.EventSequences.Concurrency.ConcurrencyScope>()));
    [Fact] void should_fail() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_report_unknown() => _batchResult.Errors.ShouldNotContain(AppendError.AppendOutcomeUnknown);
    [Fact] void should_not_probe() => _probes.ShouldEqual(0);
    [Fact] async Task should_leave_the_number_unchanged() => (await _eventSequence.GetNextSequenceNumber()).ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_leave_the_tail_unchanged() => (await _eventSequence.GetTailSequenceNumber()).ShouldEqual(EventSequenceNumber.Unavailable);
}
