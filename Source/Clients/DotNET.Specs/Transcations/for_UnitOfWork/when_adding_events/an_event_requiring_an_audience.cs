// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_events;

public class an_event_requiring_an_audience : given.a_unit_of_work
{
    Exception _single;
    Exception _batch;

    void Because()
    {
        _single = Catch.Exception(() => _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.New(), new RequiresAudience(), Causation.Unknown()));
        _batch = Catch.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, [new EventForEventSourceId(EventSourceId.New(), new RequiresAudience())], []));
    }

    [Fact] void should_fail_at_enrollment_for_a_single_event() => _single.ShouldBeOfExactType<MissingAudience>();
    [Fact] void should_fail_at_enrollment_for_a_batch() => _batch.ShouldBeOfExactType<MissingAudience>();

    [Audience]
    record RequiresAudience;
}
