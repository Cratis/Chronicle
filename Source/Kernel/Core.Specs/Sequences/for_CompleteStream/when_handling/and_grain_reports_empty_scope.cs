// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Sequences.for_CompleteStream.when_handling;

public class and_grain_reports_empty_scope : Specification
{
    IGrainFactory _grainFactory;
    CompleteStreamOutcome _result;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        var eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(eventSequence);
        eventSequence.CompleteStream(Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>())
            .Returns((Result<EventSequenceNumber, EventSequences.CompleteStreamError>)EventSequences.CompleteStreamError.EmptyScope);
    }

    async Task Because() => _result = await new CompleteStream("store", "namespace", "event-log", "transactions", "month").Handle(_grainFactory);

    [Fact] void should_report_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_empty_scope_explicitly() => _result.Error.ShouldEqual(CompleteStreamError.EmptyScope);
}
