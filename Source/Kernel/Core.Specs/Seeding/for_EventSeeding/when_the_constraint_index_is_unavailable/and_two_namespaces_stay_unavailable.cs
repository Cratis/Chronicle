// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_two_namespaces_stay_unavailable : given.an_event_seeding_grain
{
    EventSeeding _globalGrain;
    IResultAwareEventSeeding _namespaceProxy;
    SeedingResult _result;

    void Establish()
    {
        AppendManyReturns(_ => AnIndexUnavailableResult());

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(["namespace-1", "namespace-2"]));
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(namespaces);

        // Both dispatches exercise the real namespace append path. Neither appends or changes tracking.
        _namespaceProxy = Substitute.For<IResultAwareEventSeeding>();
        _namespaceProxy.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>())
            .Returns(call => _grain.SeedWithResult(call.Arg<IEnumerable<SeedingEntry>>()));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(Arg.Any<string>()).Returns(_namespaceProxy);

        _globalGrain = new EventSeeding(_state, _grainFactory, _logger, _timeProvider);
        typeof(EventSeeding).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_globalGrain, EventSeedingKey.ForGlobal("TestEventStore"));
    }

    async Task Because() => _result = await _globalGrain.SeedWithResult([AnEntry(1)]);

    [Fact] void should_share_four_delays_across_both_namespaces() => _retryDelays.ShouldEqual(Enumerable.Repeat(TimeSpan.FromSeconds(1), 4));
    [Fact] void should_not_reset_the_budget_for_the_second_namespace() => AppendAttempts.ShouldEqual(6);
    [Fact] void should_still_dispatch_to_both_namespaces() => _namespaceProxy.Received(2).SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_report_incomplete_seeding() => _result.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_not_record_any_entry_as_seeded() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_persist_unappended_entries() => _state.DidNotReceive().WriteStateAsync();
}
