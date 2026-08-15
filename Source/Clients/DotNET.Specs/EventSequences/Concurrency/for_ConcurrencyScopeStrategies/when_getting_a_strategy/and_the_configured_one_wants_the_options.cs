// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeStrategies.when_getting_a_strategy;

/// <summary>
/// The options are handed to <see cref="ConcurrencyScopeStrategies"/> directly rather than registered in the
/// container, so a strategy that reads them only sees the configured value if it is passed one. Without this the
/// opt-in would be settable and have no effect - the worst failure a configuration flag can have.
/// </summary>
public class and_the_configured_one_wants_the_options : Specification
{
    ConcurrencyScopeStrategies _strategies;
    IConcurrencyScopeStrategy _result;
    ConcurrencyScope _scope;

    void Establish()
    {
        var eventSequence = Substitute.For<IEventSequence>();
        eventSequence.GetTailSequenceNumber(
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(EventSequenceNumber.Unavailable);

        _strategies = new ConcurrencyScopeStrategies(
            new ConcurrencyOptions { CheckFirstAppendIntoAScope = true },
            new ServiceCollection().BuildServiceProvider());

        _result = _strategies.GetFor(eventSequence);
    }

    async Task Because() => _scope = await _result.GetScope(EventSourceId.New(), eventSourceType: new EventSourceType("Customer"));

    [Fact] void should_create_the_optimistic_strategy() => _result.ShouldBeOfExactType<OptimisticConcurrencyStrategy>();
    [Fact] void should_have_given_it_the_configured_options() => _scope.ExpectsNoMatchingEvent.ShouldBeTrue();
}
