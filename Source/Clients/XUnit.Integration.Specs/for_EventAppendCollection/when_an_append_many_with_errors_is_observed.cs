// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_many_with_errors_is_observed : given.an_event_append_collection
{
    AppendError _error;

    void Establish()
    {
        _error = "something went wrong";
    }

    void Because()
    {
        var correlationId = CorrelationId.New();
        var result = AppendResult.Failed(correlationId, [_error]);
        _subject.OnNext([MakeAppendedEvent(correlationId, EventSourceId.New(), new object(), [], result)]);
    }

    [Fact] void should_collect_the_event_attempt() => _collection.All.Count.ShouldEqual(1);
    [Fact] void should_have_unavailable_sequence_number() => _collection.All[0].Result.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_include_the_error() => _collection.All[0].Result.Errors.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_error() => _collection.All[0].Result.Errors.Single().ShouldEqual(_error);
    [Fact] void should_have_errors() => _collection.All[0].Result.HasErrors.ShouldBeTrue();
    [Fact] void should_not_be_successful() => _collection.All[0].Result.IsSuccess.ShouldBeFalse();
}
