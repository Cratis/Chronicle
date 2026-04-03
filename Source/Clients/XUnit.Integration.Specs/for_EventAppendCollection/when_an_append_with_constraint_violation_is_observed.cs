// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_with_constraint_violation_is_observed : given.an_event_append_collection
{
    CorrelationId _correlationId;
    ConstraintViolation _violation;
    object _event;
    CollectedEvent _collected;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _event = new object();
        _violation = new ConstraintViolation(EventTypeId.Unknown, EventSequenceNumber.First, ConstraintType.UniqueEventType, "my-constraint", "Constraint was violated", []);
    }

    void Because()
    {
        var result = AppendResult.Failed(_correlationId, [_violation]);
        _collection.OnAppend(_correlationId, EventSourceId.New(), _event, ImmutableList<Causation>.Empty, result);
        _collected = _collection.Last;
    }

    [Fact] void should_have_unavailable_sequence_number() => _collected.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_carry_the_attempted_event() => _collected.Event.ShouldEqual(_event);
    [Fact] void should_include_the_constraint_violation() => _collected.ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_constraint_violation() => _collected.ConstraintViolations.Single().ShouldEqual(_violation);
    [Fact] void should_have_constraint_violations() => _collected.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _collected.IsSuccess.ShouldBeFalse();
}
