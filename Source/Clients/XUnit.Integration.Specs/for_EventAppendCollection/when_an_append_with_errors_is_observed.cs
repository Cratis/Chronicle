// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_with_errors_is_observed : given.an_event_append_collection
{
    CorrelationId _correlationId;
    AppendError _error;
    object _event;
    CollectedEvent _collected;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _event = new object();
        _error = "something went wrong";
    }

    void Because()
    {
        var result = AppendResult.Failed(_correlationId, (IEnumerable<AppendError>)[_error]);
        _collection.OnAppend(_correlationId, EventSourceId.New(), _event, ImmutableList<Causation>.Empty, result);
        _collected = _collection.Last;
    }

    [Fact] void should_have_unavailable_sequence_number() => _collected.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_carry_the_attempted_event() => _collected.Event.ShouldEqual(_event);
    [Fact] void should_include_the_error() => _collected.Errors.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_error() => _collected.Errors.Single().ShouldEqual(_error);
    [Fact] void should_have_errors() => _collected.HasErrors.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _collected.IsSuccess.ShouldBeFalse();
}
