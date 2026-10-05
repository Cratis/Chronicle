// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class an_initial_unguarded_event_then_a_guarded_one : given.a_definition_backed_batch
{
    /// <summary>
    /// A custom strategy that guards nothing for source-id-only routing, such as the Payment stream.
    /// </summary>
    void Establish() => _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(new FirstUnguardedStrategy(new OptimisticConcurrencyStrategy(_tailSource)));

    async Task Because() => await Append([Payment(), Item("2025-01")]);

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_send_the_later_guard() => _request.ConcurrencyScopes.Single().Scope.EventStreamId.ShouldEqual("2025-01");

    class FirstUnguardedStrategy(IConcurrencyScopeStrategy inner) : IConcurrencyScopeStrategy
    {
        public Task<ConcurrencyScope> GetScope(EventSourceId eventSourceId, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, IEnumerable<EventType>? eventTypes = null) =>
            inner.GetScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType, eventTypes);

        public Task<ConcurrencyScope> GetScope(ConcurrencyDimensions dimensions, EventSourceId eventSourceId, EventStreamType? eventStreamType = null, EventStreamId? eventStreamId = null, EventSourceType? eventSourceType = null, IEnumerable<EventType>? eventTypes = null) =>
            dimensions == ConcurrencyDimensions.EventSourceId
                ? Task.FromResult(ConcurrencyScope.None)
                : inner.GetScope(dimensions, eventSourceId, eventStreamType, eventStreamId, eventSourceType, eventTypes);
    }
}
