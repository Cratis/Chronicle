// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_an_ordered_unit_accepts_equivalent_sentinels : given.a_contributed_unit
{
    void Establish() => _unitOfWork.AddEvents(EventSequenceId.Log, [new(_source, _event)], []);

    async Task Because()
    {
        Stage(_required with { EventStreamType = EventStreamType.All, EventStreamId = EventStreamId.Default, EventSourceType = EventSourceType.Default });
        await _unitOfWork.Commit();
    }

    [Fact] void should_retain_the_first_scope_on_the_strict_path() => _concurrencyScopesAppended[_source].ShouldEqual(_required);
    [Fact] void should_enroll_both_events() => _eventsAppended.Count().ShouldEqual(2);
}
