// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeStrategies.when_getting_a_strategy;

/// <summary>
/// A custom strategy written before the options existed takes only the event sequence. Handing every strategy an
/// extra argument would make its constructor unmatchable and break it at resolution time, so the options are only
/// passed to a strategy that declares it wants them. Custom strategies are a supported extension point, and they
/// keep working untouched.
/// </summary>
public class and_the_configured_one_is_a_custom_strategy : Specification
{
    ConcurrencyScopeStrategies _strategies;
    IConcurrencyScopeStrategy _result;

    void Establish() => _strategies = new ConcurrencyScopeStrategies(
        new ConcurrencyOptions { DefaultStrategy = typeof(a_strategy_taking_only_the_event_sequence), CheckFirstAppendIntoAScope = true },
        new ServiceCollection().BuildServiceProvider());

    void Because() => _result = _strategies.GetFor(Substitute.For<IEventSequence>());

    [Fact] void should_create_it() => _result.ShouldBeOfExactType<a_strategy_taking_only_the_event_sequence>();

    public class a_strategy_taking_only_the_event_sequence(IEventSequence eventSequence) : IConcurrencyScopeStrategy
    {
        public Task<ConcurrencyScope> GetScope(
            EventSourceId eventSourceId,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            IEnumerable<EventType>? eventTypes = default) =>
            Task.FromResult(ConcurrencyScope.None);

        public IEventSequence EventSequence => eventSequence;
    }
}
