// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_globally_again_after_a_failed_namespace_dispatch : given.a_global_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;
    INamespaces _namespaces;
    IEventSeeding _firstNamespaceGrain;
    IEventSeeding _secondNamespaceGrain;
    EventStoreNamespaceName _firstNamespace;
    EventStoreNamespaceName _secondNamespace;
    Exception _firstError;
    int _firstNamespaceDispatchCount;

    void Establish()
    {
        _firstNamespace = "namespace-1";
        _secondNamespace = "namespace-2";

        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null)
        ];

        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_firstNamespace, _secondNamespace]));
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(_namespaces);

        _firstNamespaceGrain = Substitute.For<IEventSeeding>();
        _secondNamespaceGrain = Substitute.For<IEventSeeding>();
        _firstNamespaceGrain.Seed(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));
        _secondNamespaceGrain.Seed(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));

        _grainFactory.GetGrain<IEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", _firstNamespace).ToString()).Returns(_firstNamespaceGrain);
        _grainFactory.GetGrain<IEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", _secondNamespace).ToString()).Returns(_secondNamespaceGrain);

        // Fail the first namespace dispatch to simulate a transient failure, then succeed on the retry.
        _firstNamespaceGrain
            .When(x => x.Seed(Arg.Any<IEnumerable<SeedingEntry>>()))
            .Do(_ =>
            {
                _firstNamespaceDispatchCount++;
                if (_firstNamespaceDispatchCount == 1)
                {
                    throw new Exception("Simulated transient namespace dispatch failure");
                }
            });
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(() => _grain.Seed(_entries));
        await _grain.Seed(_entries);
    }

    [Fact] void should_fail_the_first_attempt() => _firstError.ShouldNotBeNull();
    [Fact] void should_redispatch_to_the_failed_namespace_on_retry() => _firstNamespaceGrain.Received(2).Seed(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_dispatch_to_the_remaining_namespace_on_retry() => _secondNamespaceGrain.Received(1).Seed(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_commit_the_global_tracking_only_after_the_successful_retry() => _state.Received(1).WriteStateAsync();
}
