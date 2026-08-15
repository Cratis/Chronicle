// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy;

/// <summary>
/// The constructor taking only the event sequence is the one code written before the options existed binds to, and
/// it has to keep existing rather than become an optional argument on a wider one - a default argument is filled in
/// at the call site, so an already-compiled caller would go looking for a constructor that no longer exists. It
/// behaves as the declared default says, which is the unchecked first append.
/// </summary>
public class when_constructing_it_without_options : given.an_optimistic_concurrency_strategy
{
    ConcurrencyScope _result;

    void Establish()
    {
        _strategy = new OptimisticConcurrencyStrategy(_eventSequence);
        _eventSequence.GetTailSequenceNumber(
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because() => _result = await _strategy.GetScope(_eventSourceId, eventSourceType: new EventSourceType("Customer"));

    [Fact] void should_behave_as_the_declared_default() => _result.ExpectsNoMatchingEvent.ShouldEqual(ConcurrencyOptions.CheckFirstAppendIntoAScopeByDefault);
    [Fact] void should_not_check_the_first_append() => _result.IsIncomplete.ShouldBeTrue();
}
