// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_many_with_concurrency_violations_is_observed : given.an_event_append_collection
{
    ConcurrencyViolation _violation;

    void Establish()
    {
        _violation = new ConcurrencyViolation(EventSourceId.New(), new EventSequenceNumber(1), new EventSequenceNumber(2));
    }

    void Because()
    {
        var result = AppendManyResult.Failed(CorrelationId.New(), [_violation]);
        _collection.OnAppendMany(CorrelationId.New(), EventSourceId.New(), [new object()], ImmutableList<Causation>.Empty, result);
    }

    [Fact] void should_collect_the_event_attempt() => _collection.All.Count.ShouldEqual(1);
    [Fact] void should_have_unavailable_sequence_number() => _collection.All[0].SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_include_the_concurrency_violation() => _collection.All[0].ConcurrencyViolations.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_concurrency_violation() => _collection.All[0].ConcurrencyViolations.Single().ShouldEqual(_violation);
    [Fact] void should_have_concurrency_violations() => _collection.All[0].HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _collection.All[0].IsSuccess.ShouldBeFalse();
}
