// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_storage_reports_it_did_not_commit : given.an_append_with_settlement
{
    void Establish() => _notCommitted = true;
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_probe() => _probes.ShouldEqual(0);
    [Fact] async Task should_leave_the_number_unchanged() => (await _eventSequence.GetNextSequenceNumber()).ShouldEqual(Concepts.Events.EventSequenceNumber.First);
}
