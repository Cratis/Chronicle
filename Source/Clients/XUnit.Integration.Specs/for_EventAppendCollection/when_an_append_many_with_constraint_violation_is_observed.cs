// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_many_with_constraint_violation_is_observed : given.an_event_append_collection
{
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(EventTypeId.Unknown, EventSequenceNumber.First, ConstraintType.UniqueEventType, "my-constraint", "Constraint was violated", []);
    }

    void Because()
    {
        var correlationId = CorrelationId.New();
        var result = AppendResult.Failed(correlationId, [_violation]);
        _subject.OnNext([MakeAppendedEvent(correlationId, EventSourceId.New(), new object(), [], result)]);
    }

    [Fact] void should_collect_the_event_attempt() => _collection.All.Count.ShouldEqual(1);
    [Fact] void should_have_unavailable_sequence_number() => _collection.All[0].Result.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_include_the_constraint_violation() => _collection.All[0].Result.ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_constraint_violation() => _collection.All[0].Result.ConstraintViolations.Single().ShouldEqual(_violation);
    [Fact] void should_have_constraint_violations() => _collection.All[0].Result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _collection.All[0].Result.IsSuccess.ShouldBeFalse();
}
