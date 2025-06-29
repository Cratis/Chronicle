// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_is_successful : given.a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it
{
    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
    };

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_append_events_to_event_sequence() => _eventsAppended.ShouldContainOnly(new(_firstEventEventSourceId, _firstEvent, _firstEventCausation), new(_secondEventEventSourceId, _secondEvent, _secondEventCausation));
    [Fact] void should_have_events_in_unit_of_work() => _unitOfWork.GetEvents().ShouldContainOnly(_firstEvent, _secondEvent);
    [Fact]
    void should_have_events_in_unit_of_work_in_correct_order()
    {
        var events = _unitOfWork.GetEvents().ToArray();
        events[0].ShouldEqual(_firstEvent);
        events[1].ShouldEqual(_secondEvent);
    }

    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_be_successful() => _unitOfWork.IsSuccess.ShouldBeTrue();
}
